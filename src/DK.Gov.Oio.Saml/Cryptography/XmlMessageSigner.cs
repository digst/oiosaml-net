using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Cryptography;

internal static class XmlMessageSigner
{
    // SAML places the enveloped signature directly after the message's Issuer.
    public static void Sign(XmlDocument message, string id, X509Certificate2 certificate, string signatureUri, string digestUri)
    {
        using var key = certificate.GetRSAPrivateKey();
        var signedXml = new SignedXml(message) { SigningKey = key };
        signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = signatureUri;
        var reference = new Reference("#" + id) { DigestMethod = digestUri };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);
        signedXml.KeyInfo = new KeyInfo();
        signedXml.KeyInfo.AddClause(new KeyInfoX509Data(certificate, X509IncludeOption.EndCertOnly));
        signedXml.ComputeSignature();

        var issuer = message.DocumentElement!.GetElementsByTagName("Issuer", Saml20Constants.ASSERTION)[0]!;
        issuer.ParentNode!.InsertAfter(message.ImportNode(signedXml.GetXml(), true), issuer);
    }
}
