using System.Web;
using DK.Gov.Oio.Saml.AspNet.Controllers;
using DK.Gov.Oio.Saml.AspNet.Security;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.DependencyInjection;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DK.Gov.Oio.Saml.AspNet.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
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
            services.AddLogging();
            services.AddScoped<ISecurityContext>(sp => new SystemWebSecurityContext(
                new HttpContextWrapper(HttpContext.Current ?? throw new InvalidOperationException("No active HttpContext.")),
                sp.GetRequiredService<TimeProvider>()));
            services.AddTransient<AuthController>();
            return services;
        }
    }
}
