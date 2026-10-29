using System.Security.Claims;
using DK.Gov.Oio.Saml.AspNet.Security;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.AspNet.Tests;

public class SystemWebSecurityContextTests
{
    private readonly FakeHttpContext _http = new();

    [Fact]
    public void AnonymousUserIsNoPrincipal()
    {
        _http.User = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Null(Context().GetCurrentPrincipal());
    }

    [Fact]
    public void AuthenticatedUserIsThePrincipal()
    {
        _http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimsConstants.SessionIndex, "index")], Saml20Constants.AuthenticationScheme));
        Assert.Equal("index", Context().GetSessionIndex());
    }

    [Fact]
    public void RelayStateIsKeptInTheSession()
    {
        var context = Context();
        context.SetLoginRelayStateInSession(new PendingRelayState("token", "value"));
        Assert.Equal("value", context.GetLoginRelayStateFromSessionAndRemove()!.Value);
    }

    [Fact]
    public void MissingSessionStateIsExplained()
    {
        _http.Session = null;
        Assert.Throws<InvalidOperationException>(() => Context().SetLogoutRequestIdInSession("id"));
    }

    [Fact]
    public async Task SignInIssuesTheSamlCookie()
    {
        await Context().SignIn(new ClaimsPrincipal(new ClaimsIdentity([], Saml20Constants.AuthenticationScheme)));
        Assert.Equal(Saml20Constants.AuthenticationScheme, _http.Owin.Authentication.AuthenticationResponseGrant.Identity.AuthenticationType);
    }

    [Fact]
    public async Task SignInRecordsTheSignInTime()
    {
        await Context().SignIn(new ClaimsPrincipal(new ClaimsIdentity([], Saml20Constants.AuthenticationScheme)));
        Assert.Contains(SessionLifetime.SignedInKey, _http.Owin.Authentication.AuthenticationResponseGrant.Properties.Dictionary.Keys);
    }

    [Fact]
    public async Task SignOutEndsTheSamlCookie()
    {
        await Context().SignOut();
        Assert.Equal([Saml20Constants.AuthenticationScheme], _http.Owin.Authentication.AuthenticationResponseRevoke.AuthenticationTypes);
    }

    private SystemWebSecurityContext Context() => new(_http.Build(), TimeProvider.System);
}
