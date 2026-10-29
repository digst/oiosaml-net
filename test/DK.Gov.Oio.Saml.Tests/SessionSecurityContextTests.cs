using System.Security.Claims;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;

namespace DK.Gov.Oio.Saml.Tests;

public class SessionSecurityContextTests
{
    private readonly DictionarySecurityContext _context = new();

    [Fact]
    public void RelayStateValueWithLineBreaksSurvivesTheSession()
    {
        _context.SetLoginRelayStateInSession(new PendingRelayState("token", "line one\nline two"));
        Assert.Equal("line one\nline two", _context.GetLoginRelayStateFromSessionAndRemove()!.Value);
    }

    [Fact]
    public void RelayStateWithoutValueKeepsItsToken()
    {
        _context.SetLogoutRelayStateInSession(new PendingRelayState("token", null));
        Assert.Equal("token", _context.GetLogoutRelayStateFromSessionAndRemove()!.Token);
    }

    [Fact]
    public void RelayStateCanBeTakenOnlyOnce()
    {
        _context.SetLoginRelayStateInSession(new PendingRelayState("token", "value"));
        _context.GetLoginRelayStateFromSessionAndRemove();
        Assert.Null(_context.GetLoginRelayStateFromSessionAndRemove());
    }

    [Fact]
    public void AuthnRequestRoundTripsThroughTheSession()
    {
        _context.SetAuthnRequestInSession(TestFederation.AuthnRequest());
        Assert.Equal(TestFederation.AuthnRequestId, _context.GetAuthnRequestFromSessionAndRemove()!.ID);
    }

    [Fact]
    public void ClearingTheReturnUrlRemovesIt()
    {
        _context.SetReturnUrlInSession("/orders/42");
        _context.SetReturnUrlInSession(null);
        Assert.Null(_context.GetReturnUrlFromSessionAndRemove());
    }

    [Fact]
    public void LoggedInSubjectComesFromTheNameIdClaims()
    {
        _context.Principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimsConstants.SubjectNameId, "alice"),
            new Claim(ClaimsConstants.SubjectNameIdFormat, Saml20Constants.NameIdentifierFormats.Persistent)], "test"));
        Assert.Equal("alice", _context.GetLoggedInSubject()!.Value);
    }

    [Fact]
    public void NoLoggedInSubjectWithoutPrincipal() => Assert.Null(_context.GetLoggedInSubject());

    private sealed class DictionarySecurityContext : SessionSecurityContext
    {
        private readonly Dictionary<string, string> _session = [];

        public ClaimsPrincipal? Principal { get; set; }

        public override ClaimsPrincipal? GetCurrentPrincipal() => Principal;

        public override Task SignIn(ClaimsPrincipal claimsPrincipal) => Task.CompletedTask;

        public override Task SignOut() => Task.CompletedTask;

        protected override string? GetSessionValue(string key) => _session.GetValueOrDefault(key);

        protected override void SetSessionValue(string key, string value) => _session[key] = value;

        protected override void RemoveSessionValue(string key) => _session.Remove(key);
    }
}
