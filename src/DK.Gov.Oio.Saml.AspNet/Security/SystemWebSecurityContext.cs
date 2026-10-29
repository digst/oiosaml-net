using System.Security.Claims;
using System.Web;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;
using Microsoft.Owin.Security;

namespace DK.Gov.Oio.Saml.AspNet.Security;

// Holds on to the request's HttpContext because the core services may resume after an await without HttpContext.Current.
public class SystemWebSecurityContext(HttpContextBase httpContext, TimeProvider timeProvider) : SessionSecurityContext
{
    public override ClaimsPrincipal? GetCurrentPrincipal() =>
        httpContext.User is ClaimsPrincipal { Identity.IsAuthenticated: true } user ? user : null;

    public override Task SignIn(ClaimsPrincipal claimsPrincipal)
    {
        Authentication.SignIn(SessionLifetime.SignedInAt(timeProvider.GetUtcNow()), claimsPrincipal.Identities.ToArray());
        return Task.CompletedTask;
    }

    public override Task SignOut()
    {
        Authentication.SignOut(Saml20Constants.AuthenticationScheme);
        return Task.CompletedTask;
    }

    protected override string? GetSessionValue(string key) => Session[key] as string;

    protected override void SetSessionValue(string key, string value) => Session[key] = value;

    protected override void RemoveSessionValue(string key) => Session.Remove(key);

    private IAuthenticationManager Authentication => httpContext.GetOwinContext().Authentication;

    private HttpSessionStateBase Session => httpContext.Session
        ?? throw new InvalidOperationException("ASP.NET session state is not available; the /auth endpoints need it to correlate responses with requests.");
}
