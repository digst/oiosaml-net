using System.Security.Claims;
using DK.Gov.Oio.Saml.AspNetCore.Security;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DK.Gov.Oio.Saml.AspNetCore.Tests;

public class SessionLifetimeTests
{
    private static readonly DateTimeOffset SignedIn = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);
    private readonly FixedTime _time = new(SignedIn);
    private readonly CookieAuthenticationOptions _cookie;

    public SessionLifetimeTests()
    {
        _cookie = new CookieAuthenticationOptions { TimeProvider = _time };
        SessionLifetime.Apply(_cookie, new SessionOptions { IdleTimeout = TimeSpan.FromMinutes(20), MaximumLength = TimeSpan.FromHours(8) });
    }

    [Fact]
    public void IdleTimeoutIsTheCookieExpiry() => Assert.Equal(TimeSpan.FromMinutes(20), _cookie.ExpireTimeSpan);

    [Fact]
    public void ExpiryIsSliding() => Assert.True(_cookie.SlidingExpiration);

    [Fact]
    public async Task SessionWithinTheMaximumLengthIsKept()
    {
        _time.Now = SignedIn.AddHours(7);
        Assert.NotNull(await Validate(SessionLifetime.SignedInAt(SignedIn)));
    }

    [Fact]
    public async Task SessionPastTheMaximumLengthIsRejected()
    {
        _time.Now = SignedIn.AddHours(8);
        Assert.Null(await Validate(SessionLifetime.SignedInAt(SignedIn)));
    }

    [Fact]
    public async Task CookieWithoutSignInTimeIsRejected() => Assert.Null(await Validate(new AuthenticationProperties()));

    private async Task<ClaimsPrincipal?> Validate(AuthenticationProperties properties)
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(Mock.Of<IAuthenticationService>()).BuildServiceProvider() };
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "test")), properties, "scheme");
        var context = new CookieValidatePrincipalContext(http, new AuthenticationScheme("scheme", null, typeof(CookieAuthenticationHandler)), _cookie, ticket);
        await _cookie.Events.ValidatePrincipal(context);
        return context.Principal;
    }
}
