using DK.Gov.Oio.Saml.AspNetCore.Controllers;
using DK.Gov.Oio.Saml.AspNetCore.Interfaces;
using DK.Gov.Oio.Saml.AspNetCore.Security;
using DK.Gov.Oio.Saml.AspNetCore.Services;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.DependencyInjection;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DK.Gov.Oio.Saml.AspNetCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOioSamlCookieAuthentication()
        {
            services.AddAuthentication().AddCookie(Saml20Constants.AuthenticationScheme, o =>
            {
                o.LoginPath = new PathString("/auth/login");
                o.Cookie.SameSite = SameSiteMode.None;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });
            services.AddOptions<CookieAuthenticationOptions>(Saml20Constants.AuthenticationScheme)
                .Configure<OioSamlOptions>((cookie, options) => SessionLifetime.Apply(cookie, options.Session));
            return services;
        }

        public IServiceCollection AddOioSaml(Action<OioSamlOptions> configure)
        {
            var options = new OioSamlOptions();
            configure(options);
            return services.AddOioSaml(options);
        }

        public IServiceCollection AddOioSaml(IConfiguration configuration, Action<OioSamlOptions>? configure = null)
        {
            var options = configuration.Get<OioSamlOptions>() ?? throw new InvalidOperationException($"OIOSAML configuration section '{(configuration as IConfigurationSection)?.Path}' is missing or empty.");
            configure?.Invoke(options);
            return services.AddOioSaml(options);
        }

        public IServiceCollection AddOioSaml(OioSamlOptions options)
        {
            services.AddOioSamlCore(options);
            services.AddHostedService<CertificateMonitoringService>();
            services.AddScoped<ISecurityContext, HttpContextAccessorSecurityContext>();
            services.AddScoped<IAspNetCoreLoginService, AspNetCoreLoginService>();
            services.AddScoped<IAspNetCoreLogoutService, AspNetCoreLogoutService>();
            services.AddTransient<AuthController>();
            services.AddAntiforgery();
            return services;
        }
    }
}
