using System.Web.Mvc;
using System.Web.Routing;
using DK.Gov.Oio.Saml.AspNet.Controllers;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DK.Gov.Oio.Saml.AspNet.Tests;

public class AuthControllerTests
{
    private readonly Mock<ISaml20LoginService> _login = new();
    private readonly Mock<ISaml20LogoutService> _logout = new();
    private readonly OioSamlOptions _options = new() { ServiceProvider = { LoginRedirectUrl = "/home/secure", LogoutRedirectUrl = "/home" } };
    private readonly FakeHttpContext _http = new();
    private ILogoutParameters? _received;

    public AuthControllerTests()
    {
        _logout.Setup(l => l.ProcessSpInitiatedLogout(It.IsAny<ILogoutParameters>())).Callback<ILogoutParameters>(p => _received = p)
            .ReturnsAsync(new SignedLogoutRequest("https://idp.test/slo?SAMLRequest=x", isRedirectRequest: true));
        _logout.Setup(l => l.ProcessIdpInitiatedLogout(It.IsAny<ILogoutParameters>())).Callback<ILogoutParameters>(p => _received = p)
            .ReturnsAsync(new SignedLogoutResponse("<form/>", isRedirectResponse: false));
        _logout.Setup(l => l.ProcessIdpLogoutResponse(It.IsAny<ILogoutParameters>())).ReturnsAsync("after logout");
    }

    [Fact]
    public async Task LoginRedirectsToTheSignedRequest()
    {
        _login.Setup(l => l.GetSignedAuthnRequestString(It.IsAny<IAuthnRequestParameters>())).Returns("https://idp.test/sso?SAMLRequest=x");
        Assert.Equal("https://idp.test/sso?SAMLRequest=x", Redirect(await Controller().Login()));
    }

    [Fact]
    public async Task LoginPassesTheQueryParameters()
    {
        IAuthnRequestParameters? parameters = null;
        _login.Setup(l => l.GetSignedAuthnRequestString(It.IsAny<IAuthnRequestParameters>())).Callback<IAuthnRequestParameters>(p => parameters = p).Returns("https://idp.test/sso");
        _http.Query["ReturnUrl"] = "/orders/42";
        await Controller().Login();
        Assert.Equal("/orders/42", parameters!.ReturnUrl);
    }

    [Fact]
    public async Task LoginResponseRedirectsToTheReturnUrl() => Assert.Equal("/orders/42", Redirect(await AssertionConsumerService(new LoginResult("/orders/42", null))));

    [Fact]
    public async Task LoginResponseWithoutReturnUrlRedirectsToTheLoginRedirectUrl() =>
        Assert.Equal("/home/secure", Redirect(await AssertionConsumerService(new LoginResult(null, null))));

    [Fact]
    public async Task RelayStateIsHandedBackEscaped() =>
        Assert.Equal("/home/secure?RelayState=a%20%26%20b", Redirect(await AssertionConsumerService(new LoginResult(null, "a & b"))));

    [Fact]
    public async Task RelayStateJoinsAnExistingQueryBeforeTheFragment() =>
        Assert.Equal("/orders?tab=1&RelayState=x#top", Redirect(await AssertionConsumerService(new LoginResult("/orders?tab=1#top", "x"))));

    [Fact]
    public async Task UsersOwnLogoutIsRefusedOnGet() => Assert.Equal(405, Status(await Controller().Logout()));

    [Fact]
    public async Task UsersOwnLogoutWithoutAntiforgeryTokenIsRefused()
    {
        FakeHttpContext.Current();
        Assert.Equal(400, Status(await Controller().LogoutPost()));
    }

    [Fact]
    public async Task IdpLogoutRequestOnRedirectBindingGetsTheRawQuery()
    {
        _http.Query["SAMLRequest"] = "decoded";
        _http.RawUrl = "/auth/logout?SAMLRequest=fV%2Bx&SigAlg=x";
        await Controller().Logout();
        Assert.Equal("SAMLRequest=fV%2Bx&SigAlg=x", _received!.RawQuery);
    }

    [Fact]
    public async Task IdpLogoutRequestOnPostBindingIsAnsweredWithAPostForm()
    {
        _http.Form["SAMLRequest"] = "request";
        Assert.Equal("text/html", ((ContentResult)await Controller().LogoutPost()).ContentType);
    }

    [Fact]
    public async Task IdpLogoutResponseRedirectsToTheLogoutRedirectUrl()
    {
        _http.Query["SAMLResponse"] = "response";
        Assert.Equal("/home?RelayState=after%20logout", Redirect(await Controller().Logout()));
    }

    private Task<ActionResult> AssertionConsumerService(LoginResult result)
    {
        _login.Setup(l => l.ProcessAuthenticationResponse(It.IsAny<ISamlLoginResponse>())).ReturnsAsync(result);
        return Controller().AssertionConsumerService();
    }

    private AuthController Controller()
    {
        var controller = new AuthController(_login.Object, _logout.Object, _options, NullLogger<AuthController>.Instance);
        controller.ControllerContext = new ControllerContext(_http.Build(), new RouteData(), controller);
        return controller;
    }

    private static string Redirect(ActionResult result) => ((RedirectResult)result).Url;

    private static int Status(ActionResult result) => ((HttpStatusCodeResult)result).StatusCode;
}
