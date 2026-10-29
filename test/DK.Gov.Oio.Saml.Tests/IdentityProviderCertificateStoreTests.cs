using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Tests.Builders;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class IdentityProviderCertificateStoreTests
{
    private readonly ManualTimeProvider _time = new();
    private readonly OioSamlOptions _config = TestFederation.Options();
    private readonly ScriptedValidator _validator = new();
    private readonly List<X509Certificate2> _published = [TestCertificates.Idp];
    private readonly IdentityProviderCertificateStore _store;

    public IdentityProviderCertificateStoreTests()
    {
        var metadata = new Mock<IIdentityProviderMetadataService>();
        metadata.Setup(x => x.GetMetadata())
            .Returns(() => new IdentityProviderMetadata(TestFederation.IdpEntityId, new(Saml20Constants.ProtocolBindings.HTTP_Redirect, TestFederation.IdpSsoUrl)) { SigningCertificates = _published.Select(c => Convert.ToBase64String(c.RawData)).ToList() });
        _store = new IdentityProviderCertificateStore(_config, metadata.Object, _validator, _time);
    }

    private Task<IReadOnlyList<X509Certificate2>> Get() => _store.GetSigningCertificatesAsync(TestContext.Current.CancellationToken);

    private async Task<string> Rejected() => (await Assert.ThrowsAsync<InvalidOperationException>(Get)).Message;

    [Fact]
    public async Task ValidCertificate_IsTrusted() => Assert.Equal(TestCertificates.Idp, Assert.Single(await Get()));

    [Fact]
    public async Task RevokedCertificate_FailsClosed()
    {
        _validator.Next = CertificateStatus.Revoked;

        Assert.Contains("Revoked", await Rejected());
    }

    [Fact]
    public async Task UnknownStatusOfANeverValidatedCertificate_IsRejected()
    {
        _validator.Next = CertificateStatus.RevocationStatusUnknown;

        Assert.Contains("RevocationStatusUnknown", await Rejected());
    }

    [Fact]
    public async Task Verdict_IsReusedWithinTheCheckInterval()
    {
        await Get();
        _time.Advance(TimeSpan.FromMinutes(9));
        await Get();

        Assert.Equal(1, _validator.Calls);
    }

    [Fact]
    public async Task Verdict_IsRenewedAfterTheCheckInterval()
    {
        await Get();
        _time.Advance(TimeSpan.FromMinutes(10));
        await Get();

        Assert.Equal(2, _validator.Calls);
    }

    [Fact]
    public async Task CheckInterval_IsConfigurable()
    {
        _config.CertificateRevocation.CheckInterval = TimeSpan.FromMinutes(1);
        await Get();
        _time.Advance(TimeSpan.FromMinutes(2));
        await Get();

        Assert.Equal(2, _validator.Calls);
    }

    [Fact]
    public async Task UnknownStatusAfterValid_IsAcceptedWithinTheGracePeriod()
    {
        await Get();
        _validator.Next = CertificateStatus.RevocationStatusUnknown;
        _time.Advance(TimeSpan.FromHours(4));

        Assert.Single(await Get());
    }

    [Fact]
    public async Task UnknownStatusAfterValid_IsRejectedOnceTheGracePeriodHasPassed()
    {
        await Get();
        _validator.Next = CertificateStatus.RevocationStatusUnknown;
        _time.Advance(TimeSpan.FromHours(4) + TimeSpan.FromMinutes(1));

        Assert.Contains("RevocationStatusUnknown", await Rejected());
    }

    [Fact]
    public async Task GracePeriod_IsConfigurable()
    {
        _config.CertificateRevocation.GracePeriod = TimeSpan.FromMinutes(30);
        await Get();
        _validator.Next = CertificateStatus.RevocationStatusUnknown;
        _time.Advance(TimeSpan.FromMinutes(31));

        Assert.Contains("RevocationStatusUnknown", await Rejected());
    }

    [Fact]
    public async Task RevocationAfterValid_EndsTheGracePeriod()
    {
        await Get();
        _validator.Next = CertificateStatus.Revoked;
        _time.Advance(TimeSpan.FromMinutes(10));
        await Assert.ThrowsAsync<InvalidOperationException>(Get);
        _validator.Next = CertificateStatus.RevocationStatusUnknown;
        _time.Advance(TimeSpan.FromMinutes(10));

        Assert.Contains("RevocationStatusUnknown", await Rejected());
    }

    [Fact]
    public async Task CertificateAddedToTheMetadata_IsValidatedImmediately()
    {
        await Get();
        _published.Add(TestCertificates.Other);

        Assert.Equal(2, (await Get()).Count);
    }

    [Fact]
    public async Task CertificateExpiringBetweenChecks_IsNoLongerTrusted()
    {
        using var key = RSA.Create(2048);
        _published[0] = new CertificateRequest("CN=Short-lived IdP", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddMinutes(2));
        _config.CertificateRevocation.CheckInterval = TimeSpan.FromHours(1);
        await Get();
        _time.Advance(TimeSpan.FromMinutes(6));

        await Assert.ThrowsAsync<InvalidOperationException>(Get);
    }

    [Fact]
    public async Task Refresh_RevalidatesEvenWithinTheCheckInterval()
    {
        await Get();
        await _store.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, _validator.Calls);
    }

    [Fact]
    public async Task Refresh_ReportsACertificateKeptByTheGracePeriod()
    {
        await Get();
        _validator.Next = CertificateStatus.RevocationStatusUnknown;

        Assert.Contains("grace period", Assert.Single(await _store.RefreshAsync(TestContext.Current.CancellationToken)));
    }

    private sealed class ScriptedValidator : ICertificateValidator
    {
        public CertificateStatus Next { get; set; } = CertificateStatus.Valid;
        public int Calls { get; private set; }

        public Task<CertificateValidationResult> ValidateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new CertificateValidationResult(Next, Next == CertificateStatus.Valid ? null : "scripted"));
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
