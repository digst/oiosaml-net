using System.Security.Claims;
using DK.Gov.Oio.Saml.AspNet.Security;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace DK.Gov.Oio.Saml.AspNet.Tests;

public class SessionLifetimeTests
{
    private static readonly DateTimeOffset SignedIn = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);
    private readonly FixedClock _clock = new(SignedIn);
    private readonly CookieAuthenticationOptions _cookie;

    public SessionLifetimeTests()
    {
        _cookie = new CookieAuthenticationOptions { SystemClock = _clock };
        SessionLifetime.Apply(_cookie, new SessionOptions { IdleTimeout = TimeSpan.FromMinutes(20), MaximumLength = TimeSpan.FromHours(8) });
    }

    [Fact]
    public void IdleTimeoutIsTheCookieExpiry() => Assert.Equal(TimeSpan.FromMinutes(20), _cookie.ExpireTimeSpan);

    [Fact]
    public void ExpiryIsSliding() => Assert.True(_cookie.SlidingExpiration);

    [Fact]
    public async Task SessionWithinTheMaximumLengthIsKept()
    {
        _clock.UtcNow = SignedIn.AddHours(7);
        Assert.NotNull(await Validate(SessionLifetime.SignedInAt(SignedIn)));
    }

    [Fact]
    public async Task SessionPastTheMaximumLengthIsRejected()
    {
        _clock.UtcNow = SignedIn.AddHours(8);
        Assert.Null(await Validate(SessionLifetime.SignedInAt(SignedIn)));
    }

    [Fact]
    public async Task CookieWithoutSignInTimeIsRejected() => Assert.Null(await Validate(new AuthenticationProperties()));

    [Fact]
    public async Task HostsOwnValidationStillRuns()
    {
        var ran = false;
        var cookie = new CookieAuthenticationOptions { SystemClock = _clock, Provider = new CookieAuthenticationProvider { OnValidateIdentity = _ => { ran = true; return Task.CompletedTask; } } };
        SessionLifetime.Apply(cookie, new SessionOptions());
        await cookie.Provider.ValidateIdentity(new CookieValidateIdentityContext(new OwinContext(), Ticket(SessionLifetime.SignedInAt(SignedIn)), cookie));
        Assert.True(ran);
    }

    [Fact]
    public void ReplacedProviderIsRefused() =>
        Assert.Throws<InvalidOperationException>(() => SessionLifetime.Apply(new CookieAuthenticationOptions { Provider = new Moq.Mock<ICookieAuthenticationProvider>().Object }, new SessionOptions()));

    private async Task<ClaimsIdentity?> Validate(AuthenticationProperties properties)
    {
        var context = new CookieValidateIdentityContext(new OwinContext(), Ticket(properties), _cookie);
        await _cookie.Provider.ValidateIdentity(context);
        return context.Identity;
    }

    private static AuthenticationTicket Ticket(AuthenticationProperties properties) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "test"), properties);
}
