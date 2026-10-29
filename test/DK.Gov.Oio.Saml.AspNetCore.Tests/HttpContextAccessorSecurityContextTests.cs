using System.Security.Claims;
using DK.Gov.Oio.Saml.AspNetCore.Security;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Session;
using Moq;

namespace DK.Gov.Oio.Saml.AspNetCore.Tests;

public class HttpContextAccessorSecurityContextTests
{
    private readonly DefaultHttpContext _http = new();
    private readonly Mock<IAuthenticationService> _authentication = new();
    private AuthenticationProperties? _signedInWith;

    public HttpContextAccessorSecurityContextTests()
    {
        _http.Features.Set<ISessionFeature>(new SessionFeature { Session = new FakeSession() });
        _authentication.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, p) => _signedInWith = p).Returns(Task.CompletedTask);
    }

    [Fact]
    public void AnonymousUserIsNoPrincipal() => Assert.Null(Context().GetCurrentPrincipal());

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
    public async Task SignInRecordsTheSignInTime()
    {
        await Context().SignIn(new ClaimsPrincipal());
        Assert.Contains(SessionLifetime.SignedInKey, _signedInWith!.Items.Keys);
    }

    [Fact]
    public async Task SignOutEndsTheSamlCookie()
    {
        await Context().SignOut();
        _authentication.Verify(a => a.SignOutAsync(_http, Saml20Constants.AuthenticationScheme, null));
    }

    private HttpContextAccessorSecurityContext Context() =>
        new(new HttpContextAccessor { HttpContext = _http }, _authentication.Object, TimeProvider.System);
}
