using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Configuration;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using BcCertificate = Org.BouncyCastle.X509.X509Certificate;
using X509Certificate2 = System.Security.Cryptography.X509Certificates.X509Certificate2;

namespace DK.Gov.Oio.Saml.Certificates;

// Trust in an IdP certificate comes from the deployed metadata; this only establishes that it is in date and not revoked.
// Every location used here (issuer, OCSP responder, CRL) is named by the certificate itself, so whatever comes back
// is only believed once it is signed by the issuing CA, and the issuer only once it is a CA that signed the certificate.
public class RevocationCertificateValidator(OioSamlOptions options, TimeProvider timeProvider, HttpClient httpClient) : ICertificateValidator
{
    private const int KeyCertSign = 5;
    private const int CrlSign = 6;
    private const string OcspSigning = "1.3.6.1.5.5.7.3.9";
    // An OCSP response that neither echoes the nonce nor says when it is superseded could be an old one replayed.
    private static readonly TimeSpan MaximumAgeWithoutNextUpdate = TimeSpan.FromDays(1);
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly ConcurrentDictionary<string, BcCertificate> _issuers = new();
    private readonly ConcurrentDictionary<string, X509Crl> _crls = new();

    public RevocationCertificateValidator(OioSamlOptions options, TimeProvider timeProvider)
        : this(options, timeProvider, SharedHttpClient) { }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
    private TimeSpan Skew => options.AllowedClockSkew;

    public async Task<CertificateValidationResult> ValidateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        var bcCertificate = new X509CertificateParser().ReadCertificate(certificate.RawData);
        if (bcCertificate.NotAfter + Skew < Now)
            return new CertificateValidationResult(CertificateStatus.Expired, $"expired {bcCertificate.NotAfter:u}");
        if (bcCertificate.NotBefore - Skew > Now)
            return new CertificateValidationResult(CertificateStatus.NotYetValid, $"not valid before {bcCertificate.NotBefore:u}");
        if (!options.CertificateRevocation.Enabled) return CertificateValidationResult.Valid;

        var ocspResponders = AccessLocations(bcCertificate, AccessDescription.IdADOcsp);
        var crlLocations = CrlLocations(bcCertificate);
        if (ocspResponders.Count == 0 && crlLocations.Count == 0)
            return Unknown("certificate names no OCSP responder or CRL distribution point");
        var issuer = await GetIssuerAsync(bcCertificate, cancellationToken).ConfigureAwait(false);
        if (issuer == null) return Unknown("the issuing CA certificate could not be retrieved from the certificate's caIssuers location");

        var reasons = new List<string>();
        foreach (var responder in ocspResponders)
        {
            var result = await CheckOcspAsync(bcCertificate, issuer, responder, cancellationToken).ConfigureAwait(false);
            if (result.Status != CertificateStatus.RevocationStatusUnknown) return result;
            reasons.Add(result.Reason!);
        }
        foreach (var location in crlLocations)
        {
            var result = await CheckCrlAsync(bcCertificate, issuer, location, cancellationToken).ConfigureAwait(false);
            if (result.Status != CertificateStatus.RevocationStatusUnknown) return result;
            reasons.Add(result.Reason!);
        }
        return Unknown(string.Join("; ", reasons));
    }

    private async Task<BcCertificate?> GetIssuerAsync(BcCertificate certificate, CancellationToken cancellationToken)
    {
        foreach (var location in AccessLocations(certificate, AccessDescription.IdADCAIssuers))
        {
            if (_issuers.TryGetValue(location, out var cached) && IsIssuerOf(cached, certificate)) return cached;
            var issuer = Parse(await GetAsync(location, cancellationToken).ConfigureAwait(false), bytes => new X509CertificateParser().ReadCertificate(bytes));
            if (issuer == null || !IsIssuerOf(issuer, certificate)) continue;
            _issuers[location] = issuer;
            return issuer;
        }
        return null;
    }

    private bool IsIssuerOf(BcCertificate issuer, BcCertificate certificate) =>
        issuer.SubjectDN.Equivalent(certificate.IssuerDN)
        && issuer.GetBasicConstraints() >= 0
        && HasKeyUsage(issuer, KeyCertSign)
        && IsWithinValidity(issuer)
        && IsSignedBy(certificate, issuer);

    private async Task<CertificateValidationResult> CheckOcspAsync(BcCertificate certificate, BcCertificate issuer, string responder, CancellationToken cancellationToken)
    {
        var id = new CertificateID(CertificateID.DigestSha1, issuer, certificate.SerialNumber);
        var nonce = new byte[16];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(nonce);
        var extensions = new X509ExtensionsGenerator();
        extensions.AddExtension(OcspObjectIdentifiers.PkixOcspNonce, false, new DerOctetString(nonce));
        var generator = new OcspReqGenerator();
        generator.AddRequest(id);
        generator.SetRequestExtensions(extensions.Generate());

        var body = await PostAsync(responder, generator.Generate().GetEncoded(), "application/ocsp-request", cancellationToken).ConfigureAwait(false);
        var response = Parse(body, bytes => new OcspResp(bytes));
        if (response == null) return Unknown($"OCSP responder {responder} gave no response");
        if (response.Status != OcspRespStatus.Successful) return Unknown($"OCSP responder {responder} answered with status {response.Status}");
        if (response.GetResponseObject() is not BasicOcspResp basic) return Unknown($"OCSP responder {responder} sent no basic response");
        if (!IsSignedByIssuerOrItsResponder(basic, issuer)) return Unknown($"OCSP response from {responder} is not signed by the issuing CA or a responder it authorised");
        var echoed = basic.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNonce);
        if (echoed != null && !(X509ExtensionUtilities.FromExtensionValue(echoed) is Asn1OctetString echoedNonce && echoedNonce.GetOctets().SequenceEqual(nonce)))
            return Unknown($"OCSP response from {responder} carries another request's nonce");

        var single = basic.Responses.FirstOrDefault(r => r.GetCertID().Equals(id));
        if (single == null) return Unknown($"OCSP response from {responder} does not cover the certificate");
        if (single.ThisUpdate - Skew > Now || single.NextUpdate is { } next && next + Skew < Now
            || single.NextUpdate == null && echoed == null && single.ThisUpdate + MaximumAgeWithoutNextUpdate + Skew < Now)
            return Unknown($"OCSP response from {responder} is not current");
        return single.GetCertStatus() switch
        {
            null => CertificateValidationResult.Valid,
            RevokedStatus revoked => new CertificateValidationResult(CertificateStatus.Revoked, $"revoked {revoked.RevocationTime:u} according to OCSP responder {responder}"),
            _ => Unknown($"OCSP responder {responder} does not know the certificate"),
        };
    }

    private bool IsSignedByIssuerOrItsResponder(BasicOcspResp response, BcCertificate issuer) =>
        Verifies(() => response.Verify(issuer.GetPublicKey()))
        || response.GetCerts().Any(responder =>
            responder.IssuerDN.Equivalent(issuer.SubjectDN)
            && IsSignedBy(responder, issuer)
            && IsWithinValidity(responder)
            && responder.GetExtendedKeyUsage().Any(usage => usage.Id == OcspSigning)
            && Verifies(() => response.Verify(responder.GetPublicKey())));

    private async Task<CertificateValidationResult> CheckCrlAsync(BcCertificate certificate, BcCertificate issuer, string location, CancellationToken cancellationToken)
    {
        if (!_crls.TryGetValue(location, out var crl) || !IsCurrent(crl))
        {
            crl = Parse(await GetAsync(location, cancellationToken).ConfigureAwait(false), bytes => new X509CrlParser().ReadCrl(bytes));
            if (crl == null) return Unknown($"CRL {location} could not be retrieved");
            _crls[location] = crl;
        }
        if (!crl.IssuerDN.Equivalent(certificate.IssuerDN)) return Unknown($"CRL {location} is issued by another CA");
        if (!HasKeyUsage(issuer, CrlSign)) return Unknown("the issuing CA is not allowed to sign CRLs");
        if (!Verifies(() => crl.Verify(issuer.GetPublicKey()))) return Unknown($"CRL {location} is not signed by the issuing CA");
        if (!IsCurrent(crl)) return Unknown($"CRL {location} is not current");
        return crl.GetRevokedCertificate(certificate.SerialNumber) is { } entry
            ? new CertificateValidationResult(CertificateStatus.Revoked, $"revoked {entry.RevocationDate:u} according to CRL {location}")
            : CertificateValidationResult.Valid;
    }

    // RFC 5280 leaves nextUpdate optional, but a list that does not say when it is superseded cannot be told from a stale one.
    private bool IsCurrent(X509Crl crl) => crl.ThisUpdate - Skew <= Now && crl.NextUpdate is { } next && next + Skew >= Now;

    private bool IsWithinValidity(BcCertificate certificate) => certificate.NotBefore - Skew <= Now && certificate.NotAfter + Skew >= Now;

    private static bool HasKeyUsage(BcCertificate certificate, int bit) => certificate.GetKeyUsage() is { } usage && usage.Length > bit && usage[bit];

    private static bool IsSignedBy(BcCertificate certificate, BcCertificate issuer) => Verifies(() => certificate.Verify(issuer.GetPublicKey()));

    private static bool Verifies(Func<bool> verify) => Verifies(() => { if (!verify()) throw new SignatureException(); });

    private static bool Verifies(Action verify)
    {
        try { verify(); return true; }
        catch (Exception e) when (e is GeneralSecurityException or OcspException or ArgumentException) { return false; }
    }

    private static T? Parse<T>(byte[]? bytes, Func<byte[], T> parse) where T : class
    {
        if (bytes == null) return null;
        try { return parse(bytes); }
        catch (Exception e) when (e is System.IO.IOException or ArgumentException or InvalidCastException or GeneralSecurityException or CrlException) { return null; }
    }

    private static List<string> AccessLocations(BcCertificate certificate, DerObjectIdentifier method) =>
        HttpLocations(() => AuthorityInformationAccess.GetInstance(X509ExtensionUtilities.FromExtensionValue(certificate.GetExtensionValue(X509Extensions.AuthorityInfoAccess)))
            .GetAccessDescriptions().Where(d => d.AccessMethod.Equals(method)).Select(d => d.AccessLocation));

    private static List<string> CrlLocations(BcCertificate certificate) =>
        HttpLocations(() => CrlDistPoint.GetInstance(X509ExtensionUtilities.FromExtensionValue(certificate.GetExtensionValue(X509Extensions.CrlDistributionPoints)))
            .GetDistributionPoints().Select(p => p.DistributionPointName).Where(n => n?.Type == DistributionPointName.FullName)
            .SelectMany(n => GeneralNames.GetInstance(n.Name).GetNames()));

    // Only http(s): a certificate must not be able to make the SP read from file: or any other scheme.
    private static List<string> HttpLocations(Func<IEnumerable<GeneralName>> names)
    {
        try
        {
            return names().Where(n => n.TagNo == GeneralName.UniformResourceIdentifier).Select(n => DerIA5String.GetInstance(n.Name).GetString())
                .Where(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .ToList();
        }
        catch (Exception e) when (e is ArgumentException or InvalidCastException or NullReferenceException) { return []; }
    }

    private Task<byte[]?> GetAsync(string location, CancellationToken cancellationToken) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, location), cancellationToken);

    private Task<byte[]?> PostAsync(string location, byte[] body, string contentType, CancellationToken cancellationToken)
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, location) { Content = content }, cancellationToken);
    }

    private async Task<byte[]?> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false) : null;
            }
            catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException && !cancellationToken.IsCancellationRequested) { return null; }
        }
    }

    private static CertificateValidationResult Unknown(string reason) => new(CertificateStatus.RevocationStatusUnknown, reason);
}
