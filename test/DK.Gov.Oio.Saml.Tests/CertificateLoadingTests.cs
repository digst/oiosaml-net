using System.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Tests.Builders;

namespace DK.Gov.Oio.Saml.Tests;

public class CertificateLoadingTests
{
    [Fact]
    public void LoadsPkcs12FileWithPassword()
    {
        var path = Path.Combine(Path.GetTempPath(), $"oiosaml-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(path, TestCertificates.Sp.Export(X509ContentType.Pfx, "secret"));
        try
        {
            var loaded = new CertificateSource { FilePath = path, Password = "secret" }.Load();

            Assert.Equal(TestCertificates.Sp.Thumbprint, loaded.Thumbprint);
            Assert.True(loaded.HasPrivateKey);
            Assert.NotNull(loaded.GetRSAPrivateKey());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingFile_ReportsPath()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() => new CertificateSource { FilePath = "/no/such.pfx" }.Load());

        Assert.Contains("/no/such.pfx", e.Message);
    }

    [Fact]
    public void ThumbprintWithHiddenLeftToRightMark_IsExplained()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() => new CertificateSource { Thumbprint = "‎abc" }.Load());

        Assert.Contains("hidden left-to-right mark", e.Message);
    }

    [Fact]
    public void ThumbprintWithoutStoreLocation_IsReported()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() => new CertificateSource { Thumbprint = "0000000000000000000000000000000000000000" }.Load());

        Assert.Contains("needs storeLocation: CurrentUser or LocalMachine", e.Message);
    }

    [Fact]
    public void UnknownThumbprint_IsReported()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() => new CertificateSource { Thumbprint = "0000000000000000000000000000000000000000", StoreLocation = StoreLocation.CurrentUser }.Load());

        Assert.Contains("was not found", e.Message);
    }

    [Fact]
    public void CertificateSetInCode_IsUsed()
    {
        var config = new ServiceProviderOptions().SetCertificate(TestCertificates.Sp);

        Assert.Equal(TestCertificates.Sp.Thumbprint, config.GetCertificate().Thumbprint);
    }

    [Fact]
    public void CertificateSetInCode_TakesPrecedenceOverConfiguration()
    {
        var config = new ServiceProviderOptions { Certificate = new CertificateSource { FilePath = "/no/such.p12" } }.SetCertificate(TestCertificates.Sp);

        Assert.Equal(TestCertificates.Sp.Thumbprint, config.GetCertificate().Thumbprint);
    }

    [Fact]
    public void NoCertificate_IsReported()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() => new ServiceProviderOptions().GetCertificate());

        Assert.Contains("No service provider certificate is configured", e.Message);
    }

    [Fact]
    public void CertificateWithoutPrivateKey_IsReported()
    {
        var config = new ServiceProviderOptions().SetCertificate(TestCertificates.Sp.WithoutPrivateKey());

        var e = Assert.Throws<ConfigurationErrorsException>(() => config.GetCertificate());
        Assert.Contains("no private key", e.Message);
    }

    [Fact]
    public void ExpiredCertificate_IsReportedWithItsExpiryDate()
    {
        var expired = TestCertificates.CreateSelfSigned("Expired SP", new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero));
        var config = new ServiceProviderOptions().SetCertificate(expired);

        var e = Assert.Throws<ConfigurationErrorsException>(() => config.GetCertificate());
        Assert.Contains($"{expired.Thumbprint} (CN=Expired SP) expired 2026-01-31 12:00 UTC", e.Message);
    }

    private static readonly PbeParameters Aes256 = new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000);

    private static X509Certificate2 LoadPem(string certificatePem, string? keyPem, string? password = null)
    {
        var directory = Directory.CreateTempSubdirectory("oiosaml-pem").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "cert.pem"), certificatePem);
            if (keyPem != null) File.WriteAllText(Path.Combine(directory, "key.pem"), keyPem);
            return new CertificateSource
            {
                FilePath = Path.Combine(directory, "cert.pem"),
                KeyFilePath = keyPem == null ? null : Path.Combine(directory, "key.pem"),
                Password = password,
            }.Load();
        }
        finally { Directory.Delete(directory, true); }
    }

    private static string KeyPem(Func<RSA, string> export) => export(TestCertificates.Sp.GetRSAPrivateKey()!);

    [Fact]
    public void LoadsPemCertificateWithSeparatePkcs8Key()
    {
        var loaded = LoadPem(TestCertificates.Sp.ExportCertificatePem(), KeyPem(k => k.ExportPkcs8PrivateKeyPem()));

        Assert.Equal(TestCertificates.Sp.Thumbprint, loaded.Thumbprint);
    }

    [Fact]
    public void PemCertificate_CanSign()
    {
        var loaded = LoadPem(TestCertificates.Sp.ExportCertificatePem(), KeyPem(k => k.ExportPkcs8PrivateKeyPem()));

        Assert.NotEmpty(loaded.GetRSAPrivateKey()!.SignData([1, 2, 3], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public void LoadsPemFileHoldingCertificateAndPkcs1Key()
    {
        var loaded = LoadPem(TestCertificates.Sp.ExportCertificatePem() + "\n" + KeyPem(k => k.ExportRSAPrivateKeyPem()), null);

        Assert.True(loaded.HasPrivateKey);
    }

    [Fact]
    public void LoadsEncryptedPemKeyWithPassword()
    {
        var loaded = LoadPem(TestCertificates.Sp.ExportCertificatePem(), KeyPem(k => k.ExportEncryptedPkcs8PrivateKeyPem("secret", Aes256)), "secret");

        Assert.True(loaded.HasPrivateKey);
    }

    [Fact]
    public void EncryptedPemKeyWithoutPassword_IsReported()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() =>
            LoadPem(TestCertificates.Sp.ExportCertificatePem(), KeyPem(k => k.ExportEncryptedPkcs8PrivateKeyPem("secret", Aes256))));

        Assert.Contains("is encrypted; set password", e.Message);
    }

    [Fact]
    public void EncryptedPemKeyWithWrongPassword_IsReported()
    {
        Assert.Throws<ConfigurationErrorsException>(() =>
            LoadPem(TestCertificates.Sp.ExportCertificatePem(), KeyPem(k => k.ExportEncryptedPkcs8PrivateKeyPem("secret", Aes256)), "wrong"));
    }

    [Fact]
    public void PemKeyOfAnotherCertificate_IsReported()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() =>
            LoadPem(TestCertificates.Sp.ExportCertificatePem(), TestCertificates.Other.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("does not belong to the certificate", e.Message);
    }
}
