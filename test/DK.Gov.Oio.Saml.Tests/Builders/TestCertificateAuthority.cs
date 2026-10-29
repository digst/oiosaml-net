using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Security;
using BcCertificateStatus = Org.BouncyCastle.Ocsp.CertificateStatus;
using BigInteger = Org.BouncyCastle.Math.BigInteger;

namespace DK.Gov.Oio.Saml.Tests.Builders;

public sealed class TestCertificateAuthority : IDisposable
{
    private readonly RSA _key = RSA.Create(2048);
    private readonly List<BigInteger> _revoked = [];
    private long _crlNumber;

    public RevocationServer Server { get; } = new();
    public X509Certificate2 Certificate { get; }
    public bool StaleCrl { get; set; }
    public X509Certificate2? CrlSigner { get; set; }
    public X509Certificate2? OcspSigner { get; set; }
    public bool OcspIncludesSigner { get; set; }
    public bool OcspAnswersUnknown { get; set; }
    public bool StaleOcsp { get; set; }
    public bool OcspOmitsNextUpdate { get; set; }
    public bool OcspIgnoresNonce { get; set; }

    public TestCertificateAuthority()
    {
        var request = new CertificateRequest($"CN=Test CA {Guid.NewGuid():N}", _key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        Certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(1));
        Server.Publish("ca.cer", (_, _) => Certificate.RawData);
        Server.Publish("ca.crl", (_, _) => BuildCrl());
        Server.Publish("ocsp", (method, body) => method == "POST" ? RespondOcsp(body) : null);
    }

    public X509Certificate2 IssueOcspResponder()
    {
        var key = RSA.Create(2048);
        var request = Request($"CN=Test OCSP responder {Guid.NewGuid():N}", key, X509KeyUsageFlags.DigitalSignature);
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.9")], false));
        return request.Create(Certificate, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddDays(30), RandomNumberGenerator.GetBytes(16)).CopyWithPrivateKey(key);
    }

    public void Revoke(X509Certificate2 certificate) => _revoked.Add(DotNetUtilities.FromX509Certificate(certificate).SerialNumber);

    public void TakeCrlOffline() => Server.Withdraw("ca.crl");

    public void TakeOcspOffline() => Server.Withdraw("ocsp");

    public X509Certificate2 Issue(DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null, bool withOcsp = true, bool withCrl = true)
    {
        var key = RSA.Create(2048);
        var request = Request($"CN=Test IdP {Guid.NewGuid():N}", key, X509KeyUsageFlags.DigitalSignature);
        if (withCrl) request.CertificateExtensions.Add(CertificateRevocationListBuilder.BuildCrlDistributionPointExtension([Server.Url("ca.crl")]));
        if (withOcsp || withCrl) request.CertificateExtensions.Add(new X509AuthorityInformationAccessExtension(withOcsp ? [Server.Url("ocsp")] : null, [Server.Url("ca.cer")]));
        return request.Create(Certificate, notBefore ?? DateTimeOffset.UtcNow.AddHours(-1), notAfter ?? DateTimeOffset.UtcNow.AddDays(30), RandomNumberGenerator.GetBytes(16)).CopyWithPrivateKey(key);
    }

    private CertificateRequest Request(string subject, RSA key, X509KeyUsageFlags keyUsage)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(Certificate, true, false));
        return request;
    }

    private byte[] BuildCrl()
    {
        var builder = new CertificateRevocationListBuilder();
        foreach (var serial in _revoked) builder.AddEntry(serial.ToByteArray(), DateTimeOffset.UtcNow.AddHours(-2));
        var thisUpdate = StaleCrl ? DateTimeOffset.UtcNow.AddDays(-3) : DateTimeOffset.UtcNow.AddHours(-1);
        return builder.Build(CrlSigner ?? Certificate, Interlocked.Increment(ref _crlNumber), thisUpdate.AddDays(1), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1, thisUpdate);
    }

    private byte[] RespondOcsp(byte[] body)
    {
        var request = new OcspReq(body);
        var signer = OcspSigner ?? Certificate;
        var bcSigner = DotNetUtilities.FromX509Certificate(signer);
        var generator = new BasicOcspRespGenerator(new RespID(bcSigner.SubjectDN));
        var thisUpdate = StaleOcsp ? DateTime.UtcNow.AddDays(-3) : DateTime.UtcNow.AddMinutes(-5);
        foreach (var single in request.GetRequestList())
        {
            var id = single.GetCertID();
            var status = OcspAnswersUnknown ? new UnknownStatus()
                : _revoked.Contains(id.SerialNumber) ? new RevokedStatus(DateTime.UtcNow.AddHours(-2), CrlReason.KeyCompromise)
                : BcCertificateStatus.Good;
            generator.AddResponse(id, status, thisUpdate, OcspOmitsNextUpdate ? null : thisUpdate.AddDays(1), null);
        }
        if (!OcspIgnoresNonce && request.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNonce) is { } nonce)
        {
            var extensions = new X509ExtensionsGenerator();
            extensions.AddExtension(OcspObjectIdentifiers.PkixOcspNonce, false, nonce.GetOctets());
            generator.SetResponseExtensions(extensions.Generate());
        }
        var key = DotNetUtilities.GetRsaKeyPair(signer == Certificate ? _key : signer.GetRSAPrivateKey()!).Private;
        var response = generator.Generate(new Asn1SignatureFactory("SHA256WITHRSA", key), OcspIncludesSigner ? [bcSigner] : null, DateTime.UtcNow);
        return new OCSPRespGenerator().Generate(OCSPRespGenerator.Successful, response).GetEncoded();
    }

    public void Dispose()
    {
        Server.Dispose();
        _key.Dispose();
    }
}
