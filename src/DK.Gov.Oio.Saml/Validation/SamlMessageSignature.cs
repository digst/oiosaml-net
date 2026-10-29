using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Validation;

internal static class SamlMessageSignature
{
    public static string? Verify(ILogoutParameters parameters, XmlElement rawXml, IReadOnlyList<AsymmetricAlgorithm> keys)
    {
        if (parameters.Binding != Saml20Constants.ProtocolBindings.HTTP_Redirect) return XmlSignatureUtils.Verify(rawXml, keys);
        if (string.IsNullOrEmpty(parameters.SigAlg) || string.IsNullOrEmpty(parameters.Signature)) return "Message is not signed.";
        if (!AllowedAlgorithms.Signature.Contains(parameters.SigAlg!)) return $"[OIO-ALG-01] Signature algorithm '{parameters.SigAlg}' is not allowed.";
        byte[] signature;
        try { signature = Convert.FromBase64String(parameters.Signature!); }
        catch (FormatException) { return "Signature is not valid base64."; }
        if (SignedQuery(parameters) is not { } signedQuery) return "The raw query string is missing, repeats a SAML parameter or does not match the bound values.";
        var data = Encoding.UTF8.GetBytes(signedQuery);
        return keys.OfType<RSA>().Any(key => RsaSha256Signature.VerifyData(key, data, signature)) ? null : "Signature does not verify with any trusted key.";
    }

    // [SAMLBind 3.4.4.1] The signature covers SAMLRequest or SAMLResponse, RelayState if present, and SigAlg, in that order and
    // URL-encoded exactly as sent, so they are taken from the raw query rather than re-encoded. Model binding matches names
    // case-insensitively after URL-decoding them, so a repeated name in any spelling is refused and every signed value must decode
    // to the value the application binds; otherwise the signed and the processed message could differ.
    private static string? SignedQuery(ILogoutParameters parameters)
    {
        if (string.IsNullOrEmpty(parameters.RawQuery)) return null;
        var received = parameters.RawQuery!.Split('&')
            .Select(p => p.Split(['='], 2))
            .Select(p => (Name: WebUtility.UrlDecode(p[0]), Raw: p.Length == 2 ? p[1] : ""))
            .ToList();
        var message = parameters.LogoutRequest != null
            ? (HttpRedirectBindingConstants.SAMLRequest, parameters.LogoutRequest)
            : (HttpRedirectBindingConstants.SAMLResponse, parameters.LogoutResponse);
        var signed = new List<string>();
        foreach (var (name, bound) in new[] { message, (HttpRedirectBindingConstants.RelayState, parameters.RelayState), (HttpRedirectBindingConstants.SigAlg, parameters.SigAlg) })
        {
            var matches = received.Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1) return null;
            var raw = matches.SingleOrDefault().Raw;
            if (NullIfEmpty(raw == null ? null : WebUtility.UrlDecode(raw)) != NullIfEmpty(bound)) return null;
            if (raw != null) signed.Add(name + "=" + raw);
        }
        return string.Join("&", signed);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    public static async Task<TrustedKeys> GetTrustedKeysAsync(this IIdentityProviderCertificateStore certificateStore, CancellationToken cancellationToken)
    {
        try
        {
            var certificates = await certificateStore.GetSigningCertificatesAsync(cancellationToken).ConfigureAwait(false);
            return new TrustedKeys(certificates.Select(c => c.GetRSAPublicKey()).OfType<AsymmetricAlgorithm>().ToList(), null);
        }
        catch (Exception e) when (e is InvalidOperationException or System.IO.IOException or XmlException)
        {
            return new TrustedKeys(null, e.Message);
        }
    }
}

internal sealed class TrustedKeys(IReadOnlyList<AsymmetricAlgorithm>? keys, string? error)
{
    public IReadOnlyList<AsymmetricAlgorithm>? Keys { get; } = keys;
    public string? Error { get; } = error;
}
