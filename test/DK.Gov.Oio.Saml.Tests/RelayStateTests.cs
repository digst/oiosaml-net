using System.Collections.Specialized;
using System.Security.Claims;
using System.Web;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Utils;
using DK.Gov.Oio.Saml.Validation;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class RelayStateTests
{
    private const string AppState = "/orders/42?tab=details";
    private const string Script = "\"><script>alert(1)</script>";
    private readonly Mock<ISecurityContext> _securityContext = new();
    private readonly OioSamlOptions _options = TestFederation.Options();
    private readonly IIdentityProviderMetadataService _metadata = TestFederation.MetadataService(TestCertificates.Idp);
    private readonly IIdentityProviderCertificateStore _certificates = TestFederation.CertificateStore(TestCertificates.Idp);
    private PendingRelayState? _loginRelayState;
    private PendingRelayState? _logoutRelayState;
    private string? _returnUrl;

    public RelayStateTests()
    {
        var subject = new NameID { Value = new LogoutMessageBuilder().NameId, Format = Saml20Constants.NameIdentifierFormats.Persistent };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimsConstants.Issuer, TestFederation.IdpEntityId)], "test"));
        _securityContext.Setup(x => x.SetLoginRelayStateInSession(It.IsAny<PendingRelayState>())).Callback<PendingRelayState>(r => _loginRelayState = r);
        _securityContext.Setup(x => x.GetLoginRelayStateFromSessionAndRemove()).Returns(() => Take(ref _loginRelayState));
        _securityContext.Setup(x => x.SetLogoutRelayStateInSession(It.IsAny<PendingRelayState>())).Callback<PendingRelayState>(r => _logoutRelayState = r);
        _securityContext.Setup(x => x.GetLogoutRelayStateFromSessionAndRemove()).Returns(() => Take(ref _logoutRelayState));
        _securityContext.Setup(x => x.SetReturnUrlInSession(It.IsAny<string?>())).Callback<string?>(u => _returnUrl = u);
        _securityContext.Setup(x => x.GetReturnUrlFromSessionAndRemove()).Returns(() => Take(ref _returnUrl));
        _securityContext.Setup(x => x.GetAuthnRequestFromSessionAndRemove()).Returns(() => TestFederation.AuthnRequest());
        _securityContext.Setup(x => x.GetLogoutRequestIdFromSessionAndRemove()).Returns("_req1");
        _securityContext.Setup(x => x.GetCurrentPrincipal()).Returns(principal);
        _securityContext.Setup(x => x.GetLoggedInSubject()).Returns(subject);
        _securityContext.Setup(x => x.GetSessionIndex()).Returns("session-1");
        _securityContext.Setup(x => x.SignIn(It.IsAny<ClaimsPrincipal>())).Returns(Task.CompletedTask);
        _securityContext.Setup(x => x.SignOut()).Returns(Task.CompletedTask);
    }

    private static T? Take<T>(ref T? slot) where T : class
    {
        var value = slot;
        slot = null;
        return value;
    }

    private Saml20LoginService LoginService() => new(new SamlDecryptionService(_options),
        new Saml20AuthnRequestBuilder(_options, _metadata, [], TimeProvider.System),
        new HttpRedirectRequestBuilder(_options),
        new SamlAuthnResponseValidator(new SamlAssertionValidator(_options, TimeProvider.System), _options, _certificates),
        _securityContext.Object, new InMemoryAssertionReplayCache(TimeProvider.System), _options, TestFederation.AuditLog());

    private Saml20LogoutService LogoutService() => new(_options, _metadata,
        new HttpRedirectRequestBuilder(_options), new HttpPostRequestBuilder(_options),
        new SamlLogoutResponseValidator(_options, _certificates),
        new SamlLogoutRequestValidator(_options, _certificates, TimeProvider.System),
        _securityContext.Object, TestFederation.AuditLog(), TimeProvider.System);

    private string? SendAuthnRequest(string? relayState, string? returnUrl = null) =>
        Query(LoginService().GetSignedAuthnRequestString(new TestAuthnRequestParameters { RelayState = relayState, ReturnUrl = returnUrl }))[HttpRedirectBindingConstants.RelayState];

    private Task<LoginResult> ReceiveLoginResponse(string? relayState) =>
        LoginService().ProcessAuthenticationResponse(new TestLoginResponse(new SamlResponseBuilder().BuildBase64()) { RelayState = relayState });

    private async Task<string?> SendLogoutRequest(string? relayState) =>
        Query((await LogoutService().ProcessSpInitiatedLogout(new TestLogoutParameters { RelayState = relayState })).Request)[HttpRedirectBindingConstants.RelayState];

    private Task<string?> ReceiveLogoutResponse(string? relayState) =>
        LogoutService().ProcessIdpLogoutResponse(new LogoutMessageBuilder { InResponseTo = "_req1", RelayState = relayState }.RedirectResponse());

    private static NameValueCollection Query(string url) => HttpUtility.ParseQueryString(url.Substring(url.IndexOf('?') + 1));

    [Fact]
    public void AuthnRequest_CarriesTheIssuedTokenInsteadOfTheApplicationValue()
    {
        var sent = SendAuthnRequest(AppState);

        Assert.Equal(_loginRelayState!.Token, sent);
    }

    [Fact]
    public void AuthnRequestWithoutRelayState_StillCarriesAToken()
    {
        var sent = SendAuthnRequest(null);

        Assert.Equal(_loginRelayState!.Token, sent);
    }

    [Fact]
    public async Task LogoutRequestWithoutRelayState_StillCarriesAToken()
    {
        var sent = await SendLogoutRequest(null);

        Assert.Equal(_logoutRelayState!.Token, sent);
    }

    [Fact]
    public void IssuedToken_IsUrlSafeAndWithinTheBindingLimit() => Assert.Matches("^[A-Za-z0-9_-]{22}$", PendingRelayState.Issue(new string('x', 500)).Token);

    [Fact]
    public async Task LoginResponseWithTheIssuedToken_ReturnsTheApplicationValue() => Assert.Equal(AppState, (await ReceiveLoginResponse(SendAuthnRequest(AppState))).RelayState);

    [Fact]
    public async Task LoginResponseWithoutRelayState_ReturnsNull() => Assert.Null((await ReceiveLoginResponse(SendAuthnRequest(null))).RelayState);

    [Fact]
    public async Task LocalReturnUrl_IsReturnedAfterLogin() => Assert.Equal("/home/secure?tab=1", (await ReceiveLoginResponse(SendAuthnRequest(null, "/home/secure?tab=1"))).ReturnUrl);

    [Fact]
    public async Task ExternalReturnUrl_IsDropped() => Assert.Null((await ReceiveLoginResponse(SendAuthnRequest(null, "https://evil.test"))).ReturnUrl);

    [Fact]
    public async Task ReturnUrlFromAnEarlierLogin_IsNotReused()
    {
        SendAuthnRequest(null, "/home/secure");

        Assert.Null((await ReceiveLoginResponse(SendAuthnRequest(null))).ReturnUrl);
    }

    [Fact]
    public async Task LoginResponseWithForgedRelayState_IsRejected()
    {
        SendAuthnRequest(AppState);

        await Assert.ThrowsAsync<Saml20ValidationException>(() => ReceiveLoginResponse("https://evil.test"));
    }

    [Fact]
    public async Task LoginResponseWithForgedRelayState_DoesNotSignIn()
    {
        SendAuthnRequest(AppState);

        await Assert.ThrowsAsync<Saml20ValidationException>(() => ReceiveLoginResponse("https://evil.test"));

        _securityContext.Verify(x => x.SignIn(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task LoginResponseMissingTheIssuedRelayState_IsRejected()
    {
        SendAuthnRequest(AppState);

        await Assert.ThrowsAsync<Saml20ValidationException>(() => ReceiveLoginResponse(null));
    }

    [Fact]
    public async Task LoginResponseWithUnsolicitedRelayState_IsRejected()
    {
        SendAuthnRequest(null);

        await Assert.ThrowsAsync<Saml20ValidationException>(() => ReceiveLoginResponse("/admin"));
    }

    [Fact]
    public async Task IssuedToken_IsRedeemedOnlyOnce()
    {
        var token = SendAuthnRequest(AppState);
        await ReceiveLoginResponse(token);

        await Assert.ThrowsAsync<Saml20ValidationException>(() => ReceiveLoginResponse(token));
    }

    [Fact]
    public async Task LogoutRequest_CarriesTheIssuedToken()
    {
        var sent = await SendLogoutRequest(AppState);

        Assert.Equal(_logoutRelayState!.Token, sent);
    }

    [Fact]
    public async Task LogoutResponseWithTheIssuedToken_ReturnsTheApplicationValue() => Assert.Equal(AppState, await ReceiveLogoutResponse(await SendLogoutRequest(AppState)));

    [Fact]
    public async Task LogoutResponseWithForgedRelayState_IsRejected()
    {
        await SendLogoutRequest(AppState);

        await Assert.ThrowsAsync<Saml20ValidationException>(() => ReceiveLogoutResponse("https://evil.test"));
    }

    [Fact]
    public async Task IdpLogoutOverRedirect_EchoesTheRelayState()
    {
        var response = await LogoutService().ProcessIdpInitiatedLogout(new LogoutMessageBuilder { RelayState = "idp-state" }.RedirectRequest());

        Assert.Equal("idp-state", Query(response.Response)[HttpRedirectBindingConstants.RelayState]);
    }

    [Fact]
    public async Task IdpLogoutOverPost_EchoesTheRelayState()
    {
        var response = await LogoutService().ProcessIdpInitiatedLogout(new LogoutMessageBuilder { RelayState = "idp-state" }.PostRequest());

        Assert.Contains("<input type=\"hidden\" name=\"RelayState\" value=\"idp-state\" />", response.Response);
    }

    [Fact]
    public async Task IdpLogoutWithOversizedRelayState_IsDenied()
    {
        var response = await LogoutService().ProcessIdpInitiatedLogout(new LogoutMessageBuilder { RelayState = new string('x', 81) }.RedirectRequest());

        var logoutResponse = Serialization.DeserializeFromXmlString<LogoutResponse>(Query(response.Response)[HttpRedirectBindingConstants.SAMLResponse]!.DeflateDecompress());
        Assert.Equal(Saml20Constants.StatusCodes.RequestDenied, logoutResponse.Status.StatusCode.Value);
    }

    [Fact]
    public void PostBinding_DoesNotEmitScriptFromRelayState() => Assert.DoesNotContain("<script>alert", PostLogoutResponse(Script));

    [Fact]
    public void PostBinding_PostsTheRelayStateBackAttributeEncoded() =>
        Assert.Contains("name=\"RelayState\" value=\"&quot;&gt;&lt;script&gt;alert(1)&lt;/script&gt;\"", PostLogoutResponse(Script));

    [Fact]
    public void PostBinding_EncodesTheFormAction() =>
        Assert.Contains("action=\"https://idp.test/slo?a=1&amp;b=&quot;\"", PostLogoutResponse(null, "https://idp.test/slo?a=1&b=\""));

    [Fact]
    public void PostBindingLogoutRequest_IsSentAsSAMLRequest()
    {
        var request = new Saml20LogoutRequest(DateTime.UtcNow)
        {
            Issuer = TestFederation.SpEntityId,
            SubjectToLogOut = new NameID { Value = "alice" },
            Reason = Saml20Constants.Reasons.User,
            Destination = TestFederation.IdpSloPostUrl,
            SessionIndex = "session-1",
        };

        Assert.Contains("name=\"SAMLRequest\"", new HttpPostRequestBuilder(_options).GetSignedLogoutRequest(request, null).Request);
    }

    private string PostLogoutResponse(string? relayState, string destination = TestFederation.IdpSloPostUrl) =>
        new HttpPostRequestBuilder(_options).GetSignedLogoutResponse(new Saml20LogoutResponse(DateTime.UtcNow)
        {
            Issuer = TestFederation.SpEntityId,
            InResponseTo = "_req1",
            Destination = destination,
            StatusCode = Saml20Constants.StatusCodes.Success,
        }, relayState).Response;
}
