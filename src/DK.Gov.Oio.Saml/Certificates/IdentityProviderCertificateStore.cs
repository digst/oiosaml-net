using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;

namespace DK.Gov.Oio.Saml.Certificates;

public interface IIdentityProviderCertificateStore
{
    // Throws InvalidOperationException when none of the IdP's signing certificates is acceptable.
    Task<IReadOnlyList<X509Certificate2>> GetSigningCertificatesAsync(CancellationToken cancellationToken = default);

    // Re-validates every signing certificate of the IdP; returns a line per certificate that is not plainly valid.
    Task<IReadOnlyList<string>> RefreshAsync(CancellationToken cancellationToken = default);
}

// Keeps the latest verdict per certificate so signature validation never waits on OCSP/CRL, except for a certificate
// not seen before (e.g. after a metadata change) or when no background refresh keeps the verdicts current.
internal class IdentityProviderCertificateStore(
    OioSamlOptions options,
    IIdentityProviderMetadataService metadataService,
    ICertificateValidator validator,
    TimeProvider timeProvider) : IIdentityProviderCertificateStore
{
    private readonly ConcurrentDictionary<string, Verdict> _verdicts = new();

    public async Task<IReadOnlyList<X509Certificate2>> GetSigningCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var accepted = new List<X509Certificate2>();
        var rejected = new List<string>();
        foreach (var certificate in SigningCertificates())
        {
            var verdict = await GetVerdictAsync(certificate, false, cancellationToken).ConfigureAwait(false);
            if (IsAccepted(certificate, verdict)) accepted.Add(certificate);
            else rejected.Add(Describe(certificate, verdict));
        }
        if (accepted.Count == 0)
            throw new InvalidOperationException($"Identity provider '{options.IdentityProvider.EntityId}' has no valid signing certificate: {(rejected.Count == 0 ? "none in metadata" : string.Join("; ", rejected))}");
        return accepted;
    }

    public async Task<IReadOnlyList<string>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var problems = new List<string>();
        List<X509Certificate2> certificates;
        try { certificates = SigningCertificates(); }
        catch (Exception e) when (e is InvalidOperationException or System.IO.IOException or System.Xml.XmlException) { return [e.Message]; }
        foreach (var certificate in certificates)
        {
            var verdict = await GetVerdictAsync(certificate, true, cancellationToken).ConfigureAwait(false);
            if (!verdict.Result.IsValid)
                problems.Add($"{Describe(certificate, verdict)}{(IsAccepted(certificate, verdict) ? " (still accepted within the grace period)" : "")}");
        }
        return problems;
    }

    private List<X509Certificate2> SigningCertificates() =>
        metadataService.GetMetadata().SigningCertificates.Select(c => c.ToX509Certificate2()).ToList();

    private async Task<Verdict> GetVerdictAsync(X509Certificate2 certificate, bool force, CancellationToken cancellationToken)
    {
        var key = Convert.ToBase64String(certificate.RawData);
        var now = timeProvider.GetUtcNow();
        _verdicts.TryGetValue(key, out var previous);
        if (!force && previous != null && now - previous.CheckedAt < options.CertificateRevocation.CheckInterval) return previous;

        var result = await validator.ValidateAsync(certificate, cancellationToken).ConfigureAwait(false);
        var lastValidAt = result.IsValid ? now : result.Status == CertificateStatus.RevocationStatusUnknown ? previous?.LastValidAt : null;
        return _verdicts[key] = new Verdict(result, now, lastValidAt);
    }

    private bool IsAccepted(X509Certificate2 certificate, Verdict verdict)
    {
        var now = timeProvider.GetUtcNow();
        if (certificate.NotAfter.ToUniversalTime() + options.AllowedClockSkew < now.UtcDateTime) return false;
        return verdict.Result.IsValid
               || verdict.Result.Status == CertificateStatus.RevocationStatusUnknown && verdict.LastValidAt is { } lastValid && now - lastValid <= options.CertificateRevocation.GracePeriod;
    }

    private static string Describe(X509Certificate2 certificate, Verdict verdict) => $"{certificate.Subject} ({certificate.Thumbprint}): {verdict.Result}";

    private sealed class Verdict(CertificateValidationResult result, DateTimeOffset checkedAt, DateTimeOffset? lastValidAt)
    {
        public CertificateValidationResult Result { get; } = result;
        public DateTimeOffset CheckedAt { get; } = checkedAt;
        public DateTimeOffset? LastValidAt { get; } = lastValidAt;
    }
}
