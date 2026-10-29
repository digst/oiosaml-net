using System.Collections.Generic;
using System.Security.Cryptography.Xml;

namespace DK.Gov.Oio.Saml.Cryptography;

// [OIO-ALG-01]
internal static class AllowedAlgorithms
{
    public const string Xmlenc11 = "http://www.w3.org/2009/xmlenc11#";
    public const string RsaOaep = Xmlenc11 + "rsa-oaep";
    public const string Aes128Gcm = Xmlenc11 + "aes128-gcm";
    public const string Aes192Gcm = Xmlenc11 + "aes192-gcm";
    public const string Aes256Gcm = Xmlenc11 + "aes256-gcm";

    public static readonly HashSet<string> Digest = [SignedXml.XmlDsigSHA256Url];
    public static readonly HashSet<string> Signature = [SignedXml.XmlDsigRSASHA256Url];
    // Stricter than [OIO-ALG-01], which also allows AES-CBC: AES-GCM is more secure.
    public static readonly HashSet<string> BlockEncryption = [Aes128Gcm, Aes192Gcm, Aes256Gcm];
    public static readonly HashSet<string> KeyTransport = [EncryptedXml.XmlEncRSAOAEPUrl, RsaOaep];
    public static readonly HashSet<string> Canonicalization = [SignedXml.XmlDsigC14NTransformUrl, SignedXml.XmlDsigExcC14NTransformUrl];
    public static readonly HashSet<string> Transforms = [SignedXml.XmlDsigEnvelopedSignatureTransformUrl, SignedXml.XmlDsigC14NTransformUrl, SignedXml.XmlDsigExcC14NTransformUrl];
}
