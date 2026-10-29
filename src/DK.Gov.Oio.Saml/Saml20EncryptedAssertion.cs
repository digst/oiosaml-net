using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Utils;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace DK.Gov.Oio.Saml;

internal class Saml20EncryptedAssertion(RSA transportKey, XmlElement encryptedAssertion)
{
    private const int GcmNonceSize = 12;
    private const int GcmTagSize = 16;

    private static readonly Dictionary<string, int> KeySizes = new()
    {
        [AllowedAlgorithms.Aes128Gcm] = 16,
        [AllowedAlgorithms.Aes192Gcm] = 24,
        [AllowedAlgorithms.Aes256Gcm] = 32,
    };

    private static readonly Dictionary<string, HashAlgorithmName> Hashes = new()
    {
        [SignedXml.XmlDsigSHA1Url] = HashAlgorithmName.SHA1,
        [SignedXml.XmlDsigSHA256Url] = HashAlgorithmName.SHA256,
        [SignedXml.XmlDsigSHA384Url] = HashAlgorithmName.SHA384,
        [SignedXml.XmlDsigSHA512Url] = HashAlgorithmName.SHA512,
        [AllowedAlgorithms.Xmlenc11 + "mgf1sha1"] = HashAlgorithmName.SHA1,
        [AllowedAlgorithms.Xmlenc11 + "mgf1sha256"] = HashAlgorithmName.SHA256,
        [AllowedAlgorithms.Xmlenc11 + "mgf1sha384"] = HashAlgorithmName.SHA384,
        [AllowedAlgorithms.Xmlenc11 + "mgf1sha512"] = HashAlgorithmName.SHA512,
    };

    public XmlElement Decrypt()
    {
        if (encryptedAssertion.LocalName != "EncryptedAssertion" || encryptedAssertion.NamespaceURI != Saml20Constants.ASSERTION)
            throw new ArgumentException("Element must be a saml:EncryptedAssertion.");

        var encryptedData = new EncryptedData();
        encryptedData.LoadXml(Descendant(encryptedAssertion, "EncryptedData") ?? throw new Saml20FormatException("EncryptedAssertion has no EncryptedData."));
        var algorithm = encryptedData.EncryptionMethod?.KeyAlgorithm;
        if (algorithm == null || !AllowedAlgorithms.BlockEncryption.Contains(algorithm))
            throw new Saml20FormatException($"[OIO-ALG-01] Block encryption algorithm '{algorithm}' is not allowed; AES-GCM is required.");

        var key = DecryptSessionKey(Descendant(encryptedAssertion, "EncryptedKey") ?? throw new Saml20FormatException("EncryptedAssertion has no EncryptedKey."));
        if (key.Length != KeySizes[algorithm])
            throw new Saml20FormatException($"[OIO-ALG-01] The session key is {key.Length * 8} bits, but {algorithm} needs {KeySizes[algorithm] * 8}.");
        var plaintext = DecryptGcm(encryptedData, key);
        try { return SecureXml.Parse(Encoding.UTF8.GetString(plaintext)).DocumentElement!; }
        catch (XmlException e) { throw new Saml20FormatException("Unable to parse the decrypted assertion.", e); }
    }

    // XML Encryption 1.1 §5.2.4: the CipherValue is the 96-bit nonce, then the ciphertext, then the 128-bit tag.
    private static byte[] DecryptGcm(EncryptedData encryptedData, byte[] key)
    {
        var cipherValue = encryptedData.CipherData?.CipherValue ?? throw new Saml20FormatException("EncryptedData has no CipherValue.");
        if (cipherValue.Length < GcmNonceSize + GcmTagSize) throw new Saml20FormatException("EncryptedData CipherValue is too short for AES-GCM.");
        var gcm = new GcmBlockCipher(new AesEngine());
        gcm.Init(false, new AeadParameters(new KeyParameter(key), GcmTagSize * 8, cipherValue.Take(GcmNonceSize).ToArray()));
        var plaintext = new byte[gcm.GetOutputSize(cipherValue.Length - GcmNonceSize)];
        try
        {
            var length = gcm.ProcessBytes(cipherValue, GcmNonceSize, cipherValue.Length - GcmNonceSize, plaintext, 0);
            length += gcm.DoFinal(plaintext, length);
            return plaintext.Take(length).ToArray();
        }
        catch (InvalidCipherTextException e) { throw new Saml20FormatException("EncryptedAssertion failed AES-GCM authentication; it has been altered.", e); }
    }

    private byte[] DecryptSessionKey(XmlElement encryptedKeyElement)
    {
        var encryptedKey = new EncryptedKey();
        encryptedKey.LoadXml(encryptedKeyElement);
        var algorithm = encryptedKey.EncryptionMethod?.KeyAlgorithm;
        if (algorithm == null || !AllowedAlgorithms.KeyTransport.Contains(algorithm))
            throw new Saml20FormatException($"[OIO-ALG-01] Key transport algorithm '{algorithm}' is not allowed; RSA-OAEP is required.");
        var cipher = encryptedKey.CipherData?.CipherValue ?? throw new Saml20FormatException("EncryptedKey has no CipherValue.");
        return transportKey.Decrypt(cipher, OaepPadding(encryptedKeyElement, algorithm));
    }

    // rsa-oaep-mgf1p fixes MGF1 to SHA-1; xmlenc11 rsa-oaep defaults it to SHA-1 unless xenc11:MGF says otherwise.
    private static RSAEncryptionPadding OaepPadding(XmlElement encryptedKey, string algorithm)
    {
        var method = Descendant(encryptedKey, "EncryptionMethod");
        var digest = HashOf(method, "DigestMethod", Saml20Constants.XMLDSIG);
        var mgf = algorithm == AllowedAlgorithms.RsaOaep ? HashOf(method, "MGF", AllowedAlgorithms.Xmlenc11) : HashAlgorithmName.SHA1;
        if (digest != mgf)
            throw new Saml20FormatException($"RSA-OAEP with digest {digest.Name} and MGF1 {mgf.Name} is not supported; digest and MGF1 must use the same hash.");
        return RSAEncryptionPadding.CreateOaep(digest);
    }

    private static HashAlgorithmName HashOf(XmlElement? method, string localName, string namespaceUri)
    {
        var uri = method?.GetElementsByTagName(localName, namespaceUri).OfType<XmlElement>().FirstOrDefault()?.GetAttribute("Algorithm");
        if (string.IsNullOrEmpty(uri)) return HashAlgorithmName.SHA1;
        return Hashes.TryGetValue(uri!, out var hash) ? hash : throw new Saml20FormatException($"[OIO-ALG-01] Unsupported {localName} algorithm '{uri}'.");
    }

    private static XmlElement? Descendant(XmlElement element, string localName) =>
        element.GetElementsByTagName(localName, Saml20Constants.XENC).OfType<XmlElement>().FirstOrDefault();
}
