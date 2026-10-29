using DK.Gov.Oio.Saml.AspNetCore.Controllers;
using DK.Gov.Oio.Saml.AspNetCore.Interfaces;
using DK.Gov.Oio.Saml.AspNetCore.Model;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DK.Gov.Oio.Saml.AspNetCore.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAspNetCoreLogoutService> _logout = new();
    private readonly Mock<IAntiforgery> _antiforgery = new();
    private readonly DefaultHttpContext _http = new();
    private ILogoutParameters? _received;

    public AuthControllerTests() =>
        _logout.Setup(l => l.ProcessLogout(It.IsAny<ILogoutParameters>())).Callback<ILogoutParameters>(p => _received = p).ReturnsAsync(new OkResult());

    [Fact]
    public async Task UsersOwnLogoutIsRefusedOnGet() => Assert.Equal(StatusCodes.Status405MethodNotAllowed, ((StatusCodeResult)await Controller().Logout(new LogoutQueryParameters())).StatusCode);

    [Fact]
    public async Task UnreadableLoginFormIsABadRequest() => Assert.IsType<BadRequestResult>(await Controller().AssertionConsumerService(null));

    [Fact]
    public async Task UnreadableLogoutFormIsABadRequest() => Assert.IsType<BadRequestResult>(await Controller().Logout((LogoutFormParameters?)null));

    [Fact]
    public async Task UsersOwnLogoutWithoutAntiforgeryTokenIsRefused()
    {
        _antiforgery.Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>())).ReturnsAsync(false);
        Assert.IsType<BadRequestResult>(await Controller().Logout(new LogoutFormParameters()));
    }

    [Fact]
    public async Task UsersOwnLogoutWithAntiforgeryTokenIsProcessed()
    {
        _antiforgery.Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>())).ReturnsAsync(true);
        Assert.IsType<OkResult>(await Controller().Logout(new LogoutFormParameters()));
    }

    [Fact]
    public async Task IdpLogoutResponseOnPostSkipsTheAntiforgeryCheck()
    {
        await Controller().Logout(new LogoutFormParameters { LogoutResponse = "response" });
        _antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task IdpLogoutRequestOnRedirectBindingGetsTheRawQuery()
    {
        _http.Request.QueryString = new QueryString("?SAMLRequest=fV%2Bx&SigAlg=x");
        await Controller().Logout(new LogoutQueryParameters { LogoutRequest = "decoded" });
        Assert.Equal("SAMLRequest=fV%2Bx&SigAlg=x", _received!.RawQuery);
    }

    private AuthController Controller() =>
        new(Mock.Of<IAspNetCoreLoginService>(), _logout.Object, _antiforgery.Object, NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = _http },
        };

    [Theory]
    [InlineData(nameof(AuthController.AssertionConsumerService))]
    [InlineData(nameof(AuthController.Logout))]
    public void PostedSamlMessages_HaveABodySizeLimit(string action) =>
        Assert.Equal(1024 * 1024, typeof(AuthController).GetMethods().Where(m => m.Name == action && m.GetCustomAttributes(typeof(HttpPostAttribute), false).Length > 0)
            .Select(m => m.GetCustomAttributes(typeof(RequestSizeLimitAttribute), false).OfType<Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>().Single().MaxRequestBodySize).Single());
}
