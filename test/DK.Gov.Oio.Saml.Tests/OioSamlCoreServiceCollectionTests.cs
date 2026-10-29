using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.DependencyInjection;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class OioSamlCoreServiceCollectionTests
{
    [Fact]
    public void LoginServiceResolvesOnceTheHostAddsASecurityContext() =>
        Assert.NotNull(Provider().CreateScope().ServiceProvider.GetRequiredService<ISaml20LoginService>());

    [Fact]
    public void LogoutServiceResolvesOnceTheHostAddsASecurityContext() =>
        Assert.NotNull(Provider().CreateScope().ServiceProvider.GetRequiredService<ISaml20LogoutService>());

    [Fact]
    public void IdentityProviderCertificateRefreshResolves() =>
        Assert.NotNull(Provider().GetRequiredService<IdentityProviderCertificateRefresh>());

    [Fact]
    public void ServiceProviderCertificateExpiryResolves() =>
        Assert.NotNull(Provider().GetRequiredService<ServiceProviderCertificateExpiry>());

    [Fact]
    public void ReplayCacheIsInMemoryWithoutADistributedCache() =>
        Assert.IsType<InMemoryAssertionReplayCache>(Provider().GetRequiredService<IAssertionReplayCache>());

    [Fact]
    public void ReplayCacheUsesTheHostsDistributedCache() =>
        Assert.IsType<DistributedCacheAssertionReplayCache>(Provider(s => s.AddSingleton<IDistributedCache>(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))).GetRequiredService<IAssertionReplayCache>());

    [Fact]
    public void TimeProviderIsTheSystemClockByDefault() => Assert.Same(TimeProvider.System, Provider().GetRequiredService<TimeProvider>());

    [Fact]
    public void TimeProviderKeepsTheHostsClock()
    {
        var clock = new HostClock();

        Assert.Same(clock, Provider(s => s.AddSingleton<TimeProvider>(clock)).GetRequiredService<TimeProvider>());
    }

    private static ServiceProvider Provider(Action<IServiceCollection>? host = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddScoped(_ => Mock.Of<ISecurityContext>());
        host?.Invoke(services);
        services.AddOioSamlCore(TestFederation.Options());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private sealed class HostClock : TimeProvider;
}
