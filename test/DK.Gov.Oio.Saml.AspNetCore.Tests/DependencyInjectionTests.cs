using DK.Gov.Oio.Saml.AspNetCore.Controllers;
using DK.Gov.Oio.Saml.AspNetCore.DependencyInjection;
using DK.Gov.Oio.Saml.AspNetCore.Services;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DK.Gov.Oio.Saml.AspNetCore.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AuthControllerResolves() => Assert.NotNull(Provider().CreateScope().ServiceProvider.GetRequiredService<AuthController>());

    [Fact]
    public void CertificatesAreMonitoredInTheBackground() => Assert.Single(Provider().GetServices<IHostedService>().OfType<CertificateMonitoringService>());

    [Fact]
    public void ReplayCacheUsesTheHostsDistributedCache() =>
        Assert.IsType<DistributedCacheAssertionReplayCache>(Provider(s => s.AddDistributedMemoryCache()).GetRequiredService<IAssertionReplayCache>());

    private static ServiceProvider Provider(Action<IServiceCollection>? host = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        host?.Invoke(services);
        services.AddOioSaml(TestFederation.Options()).AddOioSamlCookieAuthentication();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
