using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using DK.Gov.Oio.Saml.AspNet.Model;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.Extensions.Logging;

namespace DK.Gov.Oio.Saml.AspNet.Controllers;

// Reads Request.Unvalidated: ASP.NET request validation would refuse a RelayState containing markup.
[AllowAnonymous]
[RoutePrefix("Auth")]
public class AuthController(
    ISaml20LoginService loginService,
    ISaml20LogoutService logoutService,
    OioSamlOptions options,
    ILogger<AuthController> logger) : Controller
{
    [Route("Login")]
    public Task<ActionResult> Login() => WithClientIp(() =>
        Task.FromResult<ActionResult>(Redirect(loginService.GetSignedAuthnRequestString(AuthnRequestParameters.From(Request.Unvalidated.QueryString)))));

    [Route("AssertionConsumerService")]
    [HttpPost]
    public Task<ActionResult> AssertionConsumerService() => WithClientIp(async () =>
    {
        if (IsTooLarge()) return new HttpStatusCodeResult(HttpStatusCode.RequestEntityTooLarge);
        var result = await loginService.ProcessAuthenticationResponse(SamlLoginResponse.From(Request.Unvalidated.Form));
        return Redirect(WithRelayState(result.ReturnUrl ?? options.ServiceProvider.LoginRedirectUrl, result.RelayState));
    });

    [Route("logout")]
    [HttpGet]
    public Task<ActionResult> Logout() => WithClientIp(async () =>
    {
        var parameters = LogoutParameters.FromQuery(Request.Unvalidated.QueryString, RawQuery());
        // Without SAML parameters this would be the user's own logout, which only a POST with an antiforgery token may start.
        if (parameters.IsSpInitiatedLogout())
            return new HttpStatusCodeResult(HttpStatusCode.MethodNotAllowed);
        return await ProcessLogout(parameters);
    });

    [Route("logout")]
    [HttpPost]
    [ActionName("Logout")]
    public Task<ActionResult> LogoutPost() => WithClientIp(async () =>
    {
        if (IsTooLarge()) return new HttpStatusCodeResult(HttpStatusCode.RequestEntityTooLarge);
        var parameters = LogoutParameters.FromForm(Request.Unvalidated.Form);
        if (parameters.IsSpInitiatedLogout() && !IsAntiforgeryTokenValid())
        {
            logger.LogWarning("Logout refused: the request has no valid antiforgery token.");
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        return await ProcessLogout(parameters);
    });

    private async Task<ActionResult> ProcessLogout(ILogoutParameters parameters)
    {
        if (parameters.IsSpInitiatedLogout())
        {
            var request = await logoutService.ProcessSpInitiatedLogout(parameters);
            return request.IsRedirectRequest ? Redirect(request.Request) : Content(request.Request, "text/html");
        }
        if (parameters.IsIdPInitiatedLogout())
        {
            var response = await logoutService.ProcessIdpInitiatedLogout(parameters);
            return response.IsRedirectResponse ? Redirect(response.Response) : Content(response.Response, "text/html");
        }
        if (parameters.IsLogoutResponse())
            return Redirect(WithRelayState(options.ServiceProvider.LogoutRedirectUrl, await logoutService.ProcessIdpLogoutResponse(parameters)));
        throw new Saml20Exception("Unhandled Logout scenario");
    }

    // Behind a proxy UserHostAddress is the proxy's unless the host rewrites REMOTE_ADDR from the forwarded header.
    private async Task<ActionResult> WithClientIp(Func<Task<ActionResult>> action)
    {
        using (logger.BeginScope("ClientIp:{ClientIp}", Request.UserHostAddress))
            return await action();
    }

    // The redirect-binding signature covers the query string as sent, before any decoding.
    private string? RawQuery()
    {
        var url = Request.RawUrl;
        var start = url.IndexOf('?');
        return start < 0 ? null : url.Substring(start + 1);
    }

    // Web.config's maxRequestLength (4 MB by default) also limits a body sent without Content-Length.
    private bool IsTooLarge() => Request.ContentLength > Extensions.Extensions.MaximumPostedMessageBytes;

    private static bool IsAntiforgeryTokenValid()
    {
        try
        {
            AntiForgery.Validate();
            return true;
        }
        catch (HttpAntiForgeryException)
        {
            return false;
        }
    }

    private static string WithRelayState(string url, string? relayState)
    {
        if (string.IsNullOrEmpty(relayState)) return url;
        var fragmentStart = url.IndexOf('#');
        var (path, fragment) = fragmentStart < 0 ? (url, "") : (url.Substring(0, fragmentStart), url.Substring(fragmentStart));
        return path + (path.IndexOf('?') < 0 ? "?" : "&") + "RelayState=" + Uri.EscapeDataString(relayState) + fragment;
    }
}
