using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace DK.Gov.Oio.Saml.Cryptography;

// The SP signs with RSA-SHA256 only, the one signature algorithm it accepts from IdPs ([OIO-ALG-01]).
internal static class RsaSha256Signature
{
    public const string SignatureUri = SignedXml.XmlDsigRSASHA256Url;

    public static byte[] SignData(RSA key, byte[] data) => key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

    public static bool VerifyData(RSA key, byte[] data, byte[] signature) => key.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

    public static void Sign(XmlDocument message, string id, X509Certificate2 certificate) =>
        XmlMessageSigner.Sign(message, id, certificate, SignatureUri, SignedXml.XmlDsigSHA256Url);
}
