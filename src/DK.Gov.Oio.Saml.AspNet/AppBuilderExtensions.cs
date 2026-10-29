using System.Web.Helpers;
using System.Web.Hosting;
using DK.Gov.Oio.Saml.AspNet.Security;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace DK.Gov.Oio.Saml.AspNet;

public static class AppBuilderExtensions
{
    // Adds the cookie the library signs users in with and starts the certificate checks; services must come from AddOioSaml.
    public static IAppBuilder UseOioSaml(this IAppBuilder app, IServiceProvider services, Action<CookieAuthenticationOptions>? configureCookie = null)
    {
        var options = services.GetRequiredService<OioSamlOptions>();
        var cookie = new CookieAuthenticationOptions
        {
            AuthenticationType = Saml20Constants.AuthenticationScheme,
            LoginPath = new PathString("/auth/login"),
            CookieSameSite = SameSiteMode.None,
            CookieSecure = CookieSecureOption.Always,
            CookieHttpOnly = true,
            // Katana's own cookie manager loses cookies that System.Web writes in the same response, such as the session cookie.
            CookieManager = new SystemWebChunkingCookieManager(),
        };
        configureCookie?.Invoke(cookie);
        SessionLifetime.Apply(cookie, options.Session);
        app.UseCookieAuthentication(cookie);
        app.UseStageMarker(PipelineStage.Authenticate);

        // MVC's antiforgery token is tied to a claim that identifies the user; the SAML identity has none of its defaults.
        if (string.IsNullOrEmpty(AntiForgeryConfig.UniqueClaimTypeIdentifier))
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimsConstants.LocalSessionId;

        var identityProviderRefresh = services.GetRequiredService<IdentityProviderCertificateRefresh>();
        var serviceProviderExpiry = services.GetRequiredService<ServiceProviderCertificateExpiry>();
        HostingEnvironment.QueueBackgroundWorkItem(async stopping =>
        {
            try { await Task.WhenAll(identityProviderRefresh.RunAsync(stopping), serviceProviderExpiry.RunAsync(stopping)); }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        });
        return app;
    }
}
