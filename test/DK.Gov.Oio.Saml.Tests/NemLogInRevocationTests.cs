using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Tests.Builders;

namespace DK.Gov.Oio.Saml.Tests;

[Trait("Category", "Integration")]
public class NemLogInRevocationTests
{
    private readonly OioSamlOptions _config = new();

    private async Task<CertificateStatus> Status(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) =>
        (await new RevocationCertificateValidator(_config, TimeProvider.System).ValidateAsync(certificate, TestContext.Current.CancellationToken)).Status;

    [Fact]
    public async Task DevTest4IdpCertificate_IsValid() => Assert.Equal(CertificateStatus.Valid, await Status(NemLogInTestCertificates.DevTest4IdpSigning));

    [Fact]
    public async Task RevokedTestCertificate_IsRevoked() => Assert.Equal(CertificateStatus.Revoked, await Status(NemLogInTestCertificates.Revoked));
}
