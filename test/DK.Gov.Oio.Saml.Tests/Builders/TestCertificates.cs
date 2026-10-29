using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DK.Gov.Oio.Saml.Tests.Builders;

public static class TestCertificates
{
    public static readonly X509Certificate2 Idp = CreateSelfSigned("Test IdP");
    public static readonly X509Certificate2 Sp = CreateSelfSigned("Test SP");
    public static readonly X509Certificate2 Other = CreateSelfSigned("Untrusted");

    extension(X509Certificate2 certificate)
    {
        public string Base64() => Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

        public X509Certificate2 WithoutPrivateKey() => X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
    }

    public static X509Certificate2 CreateSelfSigned(string subject, DateTimeOffset? notAfter = null)
    {
        var request = new CertificateRequest($"CN={subject}", RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var expires = notAfter ?? DateTimeOffset.UtcNow.AddYears(1);
        return request.CreateSelfSigned(expires.AddYears(-1), expires);
    }
}
