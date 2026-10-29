using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Tests.Builders;
using Microsoft.Extensions.Configuration;

namespace DK.Gov.Oio.Saml.Tests;

public class OioSamlOptionsTests
{
    private static readonly OioSamlOptions Bound = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .Build()
        .GetSection("oiosaml").Get<OioSamlOptions>()!;

    [Fact]
    public void ServiceProviderEntityId_IsBound() => Assert.Equal("https://saml.oiosaml4-net.dk", Bound.ServiceProvider.EntityId);

    [Fact]
    public void AssertionConsumerServiceUrl_DefaultsToTheServerRoute() =>
        Assert.Equal("https://oiosaml-net.dk:20002/auth/AssertionConsumerService", Bound.ServiceProvider.GetAssertionConsumerServiceUrl());

    [Fact]
    public void SingleLogoutServiceUrl_CanBeOverridden() => Assert.Equal("https://oiosaml-net.dk:20002/saml/slo", Bound.ServiceProvider.GetSingleLogoutServiceUrl());

    [Fact]
    public void RedirectUrls_AreBound() => Assert.Equal(("/secure", "/home"), (Bound.ServiceProvider.LoginRedirectUrl, Bound.ServiceProvider.LogoutRedirectUrl));

    [Fact]
    public void CertificateSource_IsBound()
    {
        var source = Bound.ServiceProvider.Certificate!;

        Assert.Equal(("FA29EE761C2C9B1D972728221CAA2184C0A03B44", StoreLocation.LocalMachine), (source.Thumbprint, source.StoreLocation));
    }

    [Fact]
    public void IdentityProvider_IsBound() =>
        Assert.Equal(("https://idp.example.com", "idp-metadata/idp.example.com.xml", true), (Bound.IdentityProvider.EntityId, Bound.IdentityProvider.MetadataFile, Bound.IdentityProvider.IsPassive));

    [Fact]
    public void AllowedClockSkew_IsBoundAsATimeSpan() => Assert.Equal(TimeSpan.FromMinutes(5), Bound.AllowedClockSkew);

    [Fact]
    public void LevelOfAssurance_IsBound() => Assert.Equal((false, LoAValues.High), (Bound.LevelOfAssurance.RequestGenericLoa, Bound.LevelOfAssurance.MinimumNsisLoa));

    [Fact]
    public void UnsetOverride_FallsBackToMinimumLoa() => Assert.Equal(LoAValues.Substantial, Bound.LevelOfAssurance.GetMinimumEidasLoa());

    [Fact]
    public void CertificateRevocation_IsBound() =>
        Assert.Equal((false, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1)), (Bound.CertificateRevocation.Enabled, Bound.CertificateRevocation.CheckInterval, Bound.CertificateRevocation.GracePeriod));

    [Fact]
    public void Session_IsBound() => Assert.Equal((TimeSpan.FromMinutes(20), TimeSpan.FromHours(4)), (Bound.Session.IdleTimeout, Bound.Session.MaximumLength));

    [Fact]
    public void AppSwitchReturnUrls_AreBound() => Assert.Equal([Platform.Android, Platform.iOS], Bound.AppSwitchReturnUrls.Select(x => x.Platform));

    [Fact]
    public void Defaults_MatchTheDocumentedValues()
    {
        var options = new OioSamlOptions();

        Assert.Equal((TimeSpan.FromMinutes(3), true, true, TimeSpan.FromMinutes(10), TimeSpan.FromHours(4), TimeSpan.FromMinutes(30), TimeSpan.FromHours(8)),
            (options.AllowedClockSkew, options.LevelOfAssurance.RequestGenericLoa, options.CertificateRevocation.Enabled, options.CertificateRevocation.CheckInterval, options.CertificateRevocation.GracePeriod,
                options.Session.IdleTimeout, options.Session.MaximumLength));
    }

    [Fact]
    public void TestOptions_AreValid()
    {
        var exception = Record.Exception(() => TestFederation.Options().Validate());

        Assert.Null(exception);
    }

    public static TheoryData<string, Action<OioSamlOptions>> Problems => new()
    {
        { "serviceProvider:entityId is required", o => o.ServiceProvider.EntityId = "" },
        { "serviceProvider:server is required", o => o.ServiceProvider.Server = "" },
        { "identityProvider:entityId is required", o => o.IdentityProvider.EntityId = "" },
        { "identityProvider:metadataFile is required", o => o.IdentityProvider.MetadataFile = "" },
        { "does not exist", o => o.IdentityProvider.MetadataFile = "idp-metadata/no-such-file.xml" },
        { "is for entityID 'https://idp.test', but identityProvider:entityId is 'https://other.test'", o => o.IdentityProvider.EntityId = "https://other.test" },
        { "Every appSwitchReturnUrl needs a value", o => o.AppSwitchReturnUrls.Add(new AppSwitchReturnUrl { Platform = Platform.iOS }) },
        { "'Medium' is not a level of assurance", o => o.LevelOfAssurance.MinimumNsisLoa = "Medium" },
        { "'Highest' is not a level of assurance", o => o.LevelOfAssurance.MinimumLoa = "Highest" },
        { "login:profile 'Citizen' is not a profile", o => o.Login.Profile = "Citizen" },
        { "login:attributeProfiles 'everything' is not an attribute profile", o => o.Login.AttributeProfiles.Add("everything") },
        { "checkInterval must be positive", o => o.CertificateRevocation.CheckInterval = TimeSpan.Zero },
        { "session:idleTimeout must be positive and at most 50 minutes", o => o.Session.IdleTimeout = TimeSpan.FromMinutes(51) },
        { "session:idleTimeout must be positive", o => o.Session.IdleTimeout = TimeSpan.Zero },
        { "session:maximumLength must be positive", o => o.Session.MaximumLength = TimeSpan.Zero },
        { "No service provider certificate is configured", o => o.ServiceProvider = new ServiceProviderOptions { EntityId = "https://sp.test", Server = "https://sp.test" } },
    };

    [Theory]
    [MemberData(nameof(Problems))]
    public void InvalidOptions_AreReported(string problem, Action<OioSamlOptions> breakIt)
    {
        var options = TestFederation.Options();
        breakIt(options);

        Assert.Contains(problem, Assert.Throws<ConfigurationErrorsException>(options.Validate).Message);
    }

    [Fact]
    public void Session_EndsAtTheMaximumLength()
    {
        var signedIn = new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero);

        Assert.True(new SessionOptions().HasEnded(signedIn, signedIn.AddHours(8)));
    }

    [Fact]
    public void Session_LastsUntilTheMaximumLength()
    {
        var signedIn = new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero);

        Assert.False(new SessionOptions().HasEnded(signedIn, signedIn.AddHours(8).AddSeconds(-1)));
    }
}
