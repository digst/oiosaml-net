using System.Configuration;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using DK.Gov.Oio.Saml.AspNet.DependencyInjection;
using DK.Gov.Oio.Saml.AspNet.TestServiceProvider;
using DK.Gov.Oio.Saml.AspNet.TestServiceProvider.Controllers;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace DK.Gov.Oio.Saml.AspNet.TestServiceProvider;

public class Startup
{
    public static void Configuration(IAppBuilder app)
    {
        var settings = new SampleSettings(ConfigurationManager.AppSettings["OiosamlProfile"] ?? "oiosaml4");

        var services = new ServiceCollection();
        services.AddLogging(logging => logging
            .AddDebug()
            .AddJsonConsole(o => { o.IncludeScopes = true; o.UseUtcTimestamp = true; o.TimestampFormat = "yyyy-MM-dd'T'HH':'mm':'ss.fff'Z'"; }));
        services.AddOioSaml(options =>
        {
            options.ServiceProvider.EntityId = $"https://saml.oiosaml-net.dk/{settings.OiosamlProfile}";
            options.ServiceProvider.Server = "https://localhost:20002";
            options.ServiceProvider.LoginRedirectUrl = "/home/secure";
            options.ServiceProvider.LogoutRedirectUrl = "/home";
            options.ServiceProvider.Certificate = new CertificateSource
            {
                FilePath = "certificates/serviceprovider.p12",
                Password = ConfigurationManager.AppSettings["ServiceProviderCertificatePassword"],
            };
            options.IdentityProvider.EntityId = "https://saml.test-devtest4-nemlog-in.dk";
            options.IdentityProvider.MetadataFile = $"idp-metadata/{settings.OiosamlProfile}/{settings.OiosamlProfile}-idp-devtest4-inttest-25-11-26.xml";
            options.LevelOfAssurance.RequestGenericLoa = settings.IsOiosaml4;
            // A test SP: the login form chooses the level of assurance, profiles, ForceAuthn and IsPassive.
            options.LevelOfAssurance.MinimumLoa = LoAValues.Low;
            options.Login.AllowParametersFromRequest = true;
            options.AppSwitchReturnUrls =
            [
                new AppSwitchReturnUrl { Platform = Platform.Android, Value = "Some android app link" },
                new AppSwitchReturnUrl { Platform = Platform.iOS, Value = "Some ios app link" },
            ];
        });
        services.AddSingleton(settings);
        services.AddTransient<HomeController>();
        var provider = services.BuildServiceProvider();

        DependencyResolver.SetResolver(new ServiceProviderDependencyResolver(provider));
        GlobalFilters.Filters.Add(new HandleErrorAttribute());
        // Web.config sends X-Frame-Options; MVC's own SAMEORIGIN would conflict with it.
        AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
        RouteTable.Routes.MapMvcAttributeRoutes();
        RouteTable.Routes.MapRoute("default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });

        app.UseOioSaml(provider);
    }
}
