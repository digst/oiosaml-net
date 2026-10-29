using System;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Logging;
using DK.Gov.Oio.Saml.Services;
using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DK.Gov.Oio.Saml.DependencyInjection;

public static class OioSamlCoreServiceCollectionExtensions
{
    // Everything but the host integration: the host adds its ISecurityContext, endpoints, and runs
    // IdentityProviderCertificateRefresh and ServiceProviderCertificateExpiry in the background.
    public static IServiceCollection AddOioSamlCore(this IServiceCollection services, OioSamlOptions options)
    {
        options.Validate();
        services.AddSingleton(options);

        services.AddScoped<Saml20AuthnRequestBuilder>();
        services.AddScoped<HttpRedirectRequestBuilder>();
        services.AddScoped<HttpPostRequestBuilder>();
        services.AddSingleton<IIdentityProviderMetadataService, IdentityProviderMetadataService>();
        services.AddSingleton<ICertificateValidator>(sp => new RevocationCertificateValidator(sp.GetRequiredService<OioSamlOptions>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IIdentityProviderCertificateStore, IdentityProviderCertificateStore>();
        services.AddSingleton<IdentityProviderCertificateRefresh>();
        services.AddSingleton<ServiceProviderCertificateExpiry>();
        services.AddScoped<ISaml20LoginService, Saml20LoginService>();
        services.AddScoped<ISaml20LogoutService, Saml20LogoutService>();
        services.AddScoped<ISamlDecryptionService, SamlDecryptionService>();
        services.AddScoped<IAuthnRequestProcessor, NoopAuthnRequestProcessor>();
        services.AddSingleton<IAssertionReplayCache>(sp => sp.GetService<IDistributedCache>() is { } cache
            ? new DistributedCacheAssertionReplayCache(cache)
            : new InMemoryAssertionReplayCache(sp.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AuditLog>();
        services.AddValidatorsFromAssemblyContaining<Saml20LoginService>(includeInternalTypes: true);
        return services;
    }
}
