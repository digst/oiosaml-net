using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;

namespace DK.Gov.Oio.Saml.Utils;

internal static class XmlSignatureUtils
{
    public static bool IsSigned(XmlElement element) => FindSignature(element) != null;

    /// <returns>null when the enveloped signature on <paramref name="element"/> verifies with one of <paramref name="keys"/>, otherwise the reason it does not.</returns>
    public static string? Verify(XmlElement element, IEnumerable<AsymmetricAlgorithm> keys)
    {
        var signatureElement = FindSignature(element);
        if (signatureElement == null) return "Element is not signed.";
        var id = element.GetAttribute("ID");
        if (id.Length == 0) return "Signed element has no ID attribute.";

        var signedXml = new PinnedSignedXml(element);
        try { signedXml.LoadXml(signatureElement); }
        catch (CryptographicException e) { return $"Malformed signature: {e.Message}"; }

        var info = signedXml.SignedInfo!;
        if (!AllowedAlgorithms.Canonicalization.Contains(info.CanonicalizationMethod!)) return $"Canonicalization method '{info.CanonicalizationMethod}' is not allowed.";
        if (!AllowedAlgorithms.Signature.Contains(info.SignatureMethod!)) return $"[OIO-ALG-01] Signature method '{info.SignatureMethod}' is not allowed.";
        if (info.References.Count != 1) return "Signature must contain exactly one Reference.";
        var reference = (Reference)info.References[0]!;
        if (reference.Uri != "#" + id) return $"Signature reference '{reference.Uri}' does not point to the signed element '#{id}'.";
        if (!AllowedAlgorithms.Digest.Contains(reference.DigestMethod!)) return $"[OIO-ALG-01] Digest method '{reference.DigestMethod}' is not allowed.";
        var transforms = Enumerable.Range(0, reference.TransformChain.Count).Select(i => reference.TransformChain[i].Algorithm!).ToList();
        if (transforms.Any(t => !AllowedAlgorithms.Transforms.Contains(t))) return $"Signature transforms not allowed: {string.Join(", ", transforms)}.";

        return keys.Any(key => signedXml.CheckSignature(key)) ? null : "Signature does not verify with any trusted key.";
    }

    private static XmlElement? FindSignature(XmlElement element) =>
        element.ChildNodes.OfType<XmlElement>().FirstOrDefault(c => c.LocalName == "Signature" && c.NamespaceURI == Saml20Constants.XMLDSIG);

    // Resolves the reference to the signed element itself only, so a signed copy wrapped elsewhere in the document can never satisfy it.
    private sealed class PinnedSignedXml(XmlElement element) : SignedXml(element)
    {
        private readonly XmlElement _element = element;

        public override XmlElement? GetIdElement(XmlDocument? document, string idValue) => _element.GetAttribute("ID") == idValue ? _element : null;
    }
}
