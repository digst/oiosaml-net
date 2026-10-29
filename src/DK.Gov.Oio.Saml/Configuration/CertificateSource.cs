using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using BcCertificate = Org.BouncyCastle.X509.X509Certificate;

namespace DK.Gov.Oio.Saml.Configuration;

public class CertificateSource
{
    public string? Thumbprint { get; set; }
    public StoreLocation StoreLocation { get; set; }
    // PKCS#12, or PEM holding the certificate and (unless KeyFilePath is set) its private key.
    public string? FilePath { get; set; }
    public string? KeyFilePath { get; set; }
    // Of the PKCS#12 file or the encrypted PEM key.
    public string? Password { get; set; }

    public X509Certificate2 Load() => FilePath != null ? LoadFromFile() : LoadFromStore();

    // EphemeralKeySet (32, missing from the netstandard2.0 enum) keeps the key in memory; macOS does not support it and uses a temporary keychain instead.
    private static readonly X509KeyStorageFlags KeyStorage = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? X509KeyStorageFlags.DefaultKeySet : (X509KeyStorageFlags)32;

    private X509Certificate2 LoadFromFile()
    {
        var path = ConfiguredPath.Resolve(FilePath!);
        try
        {
            return KeyFilePath != null || File.ReadAllText(path).TrimStart().StartsWith("-----BEGIN", StringComparison.Ordinal)
                ? LoadFromPem(path, ConfiguredPath.Resolve(KeyFilePath ?? FilePath!))
                : new X509Certificate2(path, Password, KeyStorage);
        }
        catch (Exception e) when (e is CryptographicException or IOException or PemException or InvalidCipherTextException)
        {
            throw new ConfigurationErrorsException($"Could not load certificate from '{path}': {e.Message}", e);
        }
    }

    // netstandard2.0 can neither parse PEM nor attach a key to a certificate, so BouncyCastle repackages the pair as PKCS#12.
    private X509Certificate2 LoadFromPem(string certificatePath, string keyPath)
    {
        var certificate = ReadPem(certificatePath).OfType<BcCertificate>().FirstOrDefault()
                          ?? throw new ConfigurationErrorsException($"'{certificatePath}' contains no PEM certificate.");
        var key = ReadPem(keyPath).Select(o => o is AsymmetricCipherKeyPair pair ? pair.Private : o).OfType<AsymmetricKeyParameter>().FirstOrDefault(k => k.IsPrivate)
                  ?? throw new ConfigurationErrorsException($"'{keyPath}' contains no PEM private key.");
        if (key is not RsaPrivateCrtKeyParameters rsa)
            throw new ConfigurationErrorsException($"The private key in '{keyPath}' is not an RSA key.");
        if (!certificate.GetPublicKey().Equals(new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent)))
            throw new ConfigurationErrorsException($"The private key in '{keyPath}' does not belong to the certificate in '{certificatePath}'.");

        var store = new Pkcs12StoreBuilder().Build();
        store.SetKeyEntry("sp", new AsymmetricKeyEntry(key), [new X509CertificateEntry(certificate)]);
        var password = Guid.NewGuid().ToString("N");
        using var pkcs12 = new MemoryStream();
        store.Save(pkcs12, password.ToCharArray(), new SecureRandom());
        return new X509Certificate2(pkcs12.ToArray(), password, KeyStorage);
    }

    private List<object> ReadPem(string path)
    {
        var text = File.ReadAllText(path);
        // PKCS#8 "ENCRYPTED PRIVATE KEY" and OpenSSL's "Proc-Type: 4,ENCRYPTED"; BouncyCastle reports either only as a generic PemException.
        if (Password == null && text.Contains("ENCRYPTED"))
            throw new ConfigurationErrorsException($"The private key in '{path}' is encrypted; set password.");
        var pem = new PemReader(new StringReader(text), Password == null ? null : new PemPassword(Password));
        var objects = new List<object>();
        for (var o = pem.ReadObject(); o != null; o = pem.ReadObject()) objects.Add(o);
        return objects;
    }

    private sealed class PemPassword(string password) : IPasswordFinder
    {
        public char[] GetPassword() => password.ToCharArray();
    }

    private X509Certificate2 LoadFromStore()
    {
        if (string.IsNullOrEmpty(Thumbprint))
            throw new ConfigurationErrorsException("A service provider certificate needs either filePath or thumbprint.");
        if (Thumbprint![0] == '\u200E')
            throw new ConfigurationErrorsException("Thumbprint starts with a hidden left-to-right mark copied from the certificate MMC snap-in. Remove it.");
        // Unset, the enum is 0, which X509Store refuses with an unexplained "Illegal enum value".
        if (StoreLocation is not (StoreLocation.CurrentUser or StoreLocation.LocalMachine))
            throw new ConfigurationErrorsException($"Certificate with thumbprint {Thumbprint} needs storeLocation: CurrentUser or LocalMachine.");
        using var store = new X509Store(StoreName.My, StoreLocation);
        store.Open(OpenFlags.ReadOnly);
        var found = store.Certificates.Find(X509FindType.FindByThumbprint, Thumbprint, validOnly: false);
        return found.Count switch
        {
            0 => throw new ConfigurationErrorsException($"Certificate with thumbprint {Thumbprint} was not found in {StoreLocation}/{StoreName.My}."),
            1 => found[0],
            _ => throw new ConfigurationErrorsException($"Found {found.Count} certificates with thumbprint {Thumbprint} in {StoreLocation}/{StoreName.My}."),
        };
    }
}
