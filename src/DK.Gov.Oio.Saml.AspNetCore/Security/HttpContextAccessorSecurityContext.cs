using System.Security.Claims;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace DK.Gov.Oio.Saml.AspNetCore.Security;

public class HttpContextAccessorSecurityContext(IHttpContextAccessor httpContextAccessor, IAuthenticationService authenticationService, TimeProvider timeProvider) : SessionSecurityContext
{
    public override ClaimsPrincipal? GetCurrentPrincipal() =>
        httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } user ? user : null;

    public override Task SignIn(ClaimsPrincipal claimsPrincipal) => authenticationService.SignInAsync(HttpContext, Saml20Constants.AuthenticationScheme, claimsPrincipal, SessionLifetime.SignedInAt(timeProvider.GetUtcNow()));

    public override Task SignOut() => authenticationService.SignOutAsync(HttpContext, Saml20Constants.AuthenticationScheme, null);

    protected override string? GetSessionValue(string key) => Session.GetString(key);

    protected override void SetSessionValue(string key, string value) => Session.SetString(key, value);

    protected override void RemoveSessionValue(string key) => Session.Remove(key);

    private HttpContext HttpContext => httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext.");

    private ISession Session => HttpContext.Session;
}
