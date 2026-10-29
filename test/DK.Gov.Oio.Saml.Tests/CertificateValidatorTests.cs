using System.Security.Cryptography.X509Certificates;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Tests.Builders;

namespace DK.Gov.Oio.Saml.Tests;

public sealed class CertificateValidatorTests : IDisposable
{
    private readonly TestCertificateAuthority _ca = new();
    private readonly OioSamlOptions _config = new();

    private async Task<CertificateValidationResult> Validate(X509Certificate2 certificate) =>
        await new RevocationCertificateValidator(_config, TimeProvider.System).ValidateAsync(certificate, TestContext.Current.CancellationToken);

    private async Task<CertificateStatus> Status(X509Certificate2 certificate) => (await Validate(certificate)).Status;

    public void Dispose() => _ca.Dispose();

    [Fact]
    public async Task GoodOcspAnswer_IsValid() => Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue(withCrl: false)));

    [Fact]
    public async Task RevokedOcspAnswer_IsRevoked()
    {
        var certificate = _ca.Issue(withCrl: false);
        _ca.Revoke(certificate);

        Assert.Equal(CertificateStatus.Revoked, await Status(certificate));
    }

    [Fact]
    public async Task OcspRequest_IsPosted()
    {
        await Validate(_ca.Issue(withCrl: false));

        Assert.Contains("POST /ocsp", _ca.Server.Requests);
    }

    [Fact]
    public async Task OcspAnswer_IsUsedWithoutFetchingTheCrl()
    {
        await Validate(_ca.Issue());

        Assert.DoesNotContain("GET /ca.crl", _ca.Server.Requests);
    }

    [Fact]
    public async Task OcspAnsweredByAuthorisedResponder_IsValid()
    {
        _ca.OcspSigner = _ca.IssueOcspResponder();
        _ca.OcspIncludesSigner = true;

        Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue(withCrl: false)));
    }

    [Fact]
    public async Task OcspSignedByAnotherCa_IsNotBelieved()
    {
        using var other = new TestCertificateAuthority();
        _ca.OcspSigner = other.Certificate;
        _ca.OcspIncludesSigner = true;
        var certificate = _ca.Issue(withCrl: false);
        _ca.Revoke(certificate);

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(certificate));
    }

    [Fact]
    public async Task StaleOcspAnswer_IsNotBelieved()
    {
        _ca.StaleOcsp = true;

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(_ca.Issue(withCrl: false)));
    }

    [Fact]
    public async Task OldOcspAnswerWithoutNextUpdateOrNonce_IsNotBelieved()
    {
        (_ca.StaleOcsp, _ca.OcspOmitsNextUpdate, _ca.OcspIgnoresNonce) = (true, true, true);

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(_ca.Issue(withCrl: false)));
    }

    [Fact]
    public async Task OldOcspAnswerWithoutNextUpdate_IsBelievedWhenItEchoesTheNonce()
    {
        (_ca.StaleOcsp, _ca.OcspOmitsNextUpdate) = (true, true);

        Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue(withCrl: false)));
    }

    [Fact]
    public async Task RecentOcspAnswerWithoutNextUpdateOrNonce_IsValid()
    {
        (_ca.OcspOmitsNextUpdate, _ca.OcspIgnoresNonce) = (true, true);

        Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue(withCrl: false)));
    }

    [Fact]
    public async Task UnreachableOcsp_FallsBackToTheCrl()
    {
        var certificate = _ca.Issue();
        _ca.Revoke(certificate);
        _ca.TakeOcspOffline();

        Assert.Equal(CertificateStatus.Revoked, await Status(certificate));
    }

    [Fact]
    public async Task UnknownOcspAnswer_FallsBackToTheCrl()
    {
        _ca.OcspAnswersUnknown = true;

        Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue()));
    }

    [Fact]
    public async Task CertificateNotOnTheCrl_IsValid() => Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue(withOcsp: false)));

    [Fact]
    public async Task CertificateOnTheCrl_IsRevoked()
    {
        var certificate = _ca.Issue(withOcsp: false);
        _ca.Revoke(certificate);

        Assert.Equal(CertificateStatus.Revoked, await Status(certificate));
    }

    [Fact]
    public async Task StaleCrl_IsNotBelieved()
    {
        _ca.StaleCrl = true;

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(_ca.Issue(withOcsp: false)));
    }

    [Fact]
    public async Task CrlSignedByAnotherCa_IsNotBelieved()
    {
        using var other = new TestCertificateAuthority();
        _ca.CrlSigner = other.Certificate;

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(_ca.Issue(withOcsp: false)));
    }

    [Fact]
    public async Task NoRevocationSourceReachable_LeavesTheStatusUnknown()
    {
        var certificate = _ca.Issue();
        _ca.TakeOcspOffline();
        _ca.TakeCrlOffline();

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(certificate));
    }

    [Fact]
    public async Task CertificateWithoutRevocationSource_LeavesTheStatusUnknown() =>
        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(_ca.Issue(withOcsp: false, withCrl: false)));

    [Fact]
    public async Task IssuerLocationServingAnotherCa_LeavesTheStatusUnknown()
    {
        using var other = new TestCertificateAuthority();
        _ca.Server.Publish("ca.cer", (_, _) => other.Certificate.RawData);

        Assert.Equal(CertificateStatus.RevocationStatusUnknown, await Status(_ca.Issue()));
    }

    [Fact]
    public async Task ExpiredCertificate_IsExpired() =>
        Assert.Equal(CertificateStatus.Expired, await Status(_ca.Issue(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1))));

    [Fact]
    public async Task CertificateExpiredWithinClockSkew_IsValid() =>
        Assert.Equal(CertificateStatus.Valid, await Status(_ca.Issue(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddMinutes(-1))));

    [Fact]
    public async Task FutureCertificate_IsNotYetValid() =>
        Assert.Equal(CertificateStatus.NotYetValid, await Status(_ca.Issue(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(10))));

    [Fact]
    public async Task WithRevocationCheckingDisabled_OnlyTheValidityPeriodCounts()
    {
        _config.CertificateRevocation.Enabled = false;

        Assert.Equal(CertificateStatus.Valid, await Status(TestCertificates.Idp));
    }
}
