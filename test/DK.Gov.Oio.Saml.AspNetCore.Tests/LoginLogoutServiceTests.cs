using DK.Gov.Oio.Saml.AspNetCore.Model;
using DK.Gov.Oio.Saml.AspNetCore.Services;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DK.Gov.Oio.Saml.AspNetCore.Tests;

public class LoginLogoutServiceTests
{
    private readonly Mock<ISaml20LoginService> _login = new();
    private readonly Mock<ISaml20LogoutService> _logout = new();
    private readonly OioSamlOptions _options = new() { ServiceProvider = { LoginRedirectUrl = "/home/secure", LogoutRedirectUrl = "/home" } };

    [Fact]
    public void LoginRedirectsToTheSignedRequest()
    {
        _login.Setup(l => l.GetSignedAuthnRequestString(It.IsAny<IAuthnRequestParameters>())).Returns("https://idp.test/sso?SAMLRequest=x");
        Assert.Equal("https://idp.test/sso?SAMLRequest=x", Url(new AspNetCoreLoginService(_login.Object, _options).InitiateLogin(new AuthnRequestParameters())));
    }

    [Fact]
    public async Task LoginResponseRedirectsToTheReturnUrl() => Assert.Equal("/orders/42", Url(await LoginResponse(new LoginResult("/orders/42", null))));

    [Fact]
    public async Task LoginResponseWithoutReturnUrlRedirectsToTheLoginRedirectUrl() => Assert.Equal("/home/secure", Url(await LoginResponse(new LoginResult(null, null))));

    [Fact]
    public async Task RelayStateIsHandedBackEscaped() => Assert.Equal("/home/secure?RelayState=a%20%26%20b", Url(await LoginResponse(new LoginResult(null, "a & b"))));

    [Fact]
    public async Task SpLogoutOnRedirectBindingRedirects()
    {
        _logout.Setup(l => l.ProcessSpInitiatedLogout(It.IsAny<ILogoutParameters>())).ReturnsAsync(new SignedLogoutRequest("https://idp.test/slo?SAMLRequest=x", isRedirectRequest: true));
        Assert.Equal("https://idp.test/slo?SAMLRequest=x", Url(await Logout(new LogoutFormParameters())));
    }

    [Fact]
    public async Task IdpLogoutOnPostBindingAnswersWithAPostForm()
    {
        _logout.Setup(l => l.ProcessIdpInitiatedLogout(It.IsAny<ILogoutParameters>())).ReturnsAsync(new SignedLogoutResponse("<form/>", isRedirectResponse: false));
        Assert.Equal("text/html", ((ContentResult)await Logout(new LogoutFormParameters { LogoutRequest = "request" })).ContentType);
    }

    [Fact]
    public async Task IdpLogoutResponseRedirectsToTheLogoutRedirectUrl()
    {
        _logout.Setup(l => l.ProcessIdpLogoutResponse(It.IsAny<ILogoutParameters>())).ReturnsAsync("after logout");
        Assert.Equal("/home?RelayState=after%20logout", Url(await Logout(new LogoutQueryParameters { LogoutResponse = "response" })));
    }

    private Task<IActionResult> LoginResponse(LoginResult result)
    {
        _login.Setup(l => l.ProcessAuthenticationResponse(It.IsAny<ISamlLoginResponse>())).ReturnsAsync(result);
        return new AspNetCoreLoginService(_login.Object, _options).ProcessLoginResponse(new SamlLoginResponse());
    }

    private Task<IActionResult> Logout(ILogoutParameters parameters) => new AspNetCoreLogoutService(_logout.Object, _options).ProcessLogout(parameters);

    private static string Url(IActionResult result) => ((RedirectResult)result).Url;
}
