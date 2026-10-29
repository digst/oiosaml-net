using System.Security.Claims;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Logging;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class AuditLogTests
{
    private const string Privileges = "PGJwcDpQcml2aWxlZ2VMaXN0Lz4=";
    private readonly FakeLogCollector _log = new();
    private readonly Mock<ISecurityContext> _securityContext = new();
    private readonly OioSamlOptions _options = TestFederation.Options();
    private readonly LogoutMessageBuilder _logoutMessage = new();
    private AuthnRequest? _sentAuthnRequest;
    private string? _sentLogoutRequestId;
    private ClaimsPrincipal? _signedIn;

    public AuditLogTests()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimsConstants.Issuer, TestFederation.IdpEntityId), new Claim(ClaimsConstants.LocalSessionId, "local-1")], "test"));
        _securityContext.Setup(x => x.SetAuthnRequestInSession(It.IsAny<AuthnRequest>())).Callback<AuthnRequest>(r => _sentAuthnRequest = r);
        _securityContext.Setup(x => x.GetAuthnRequestFromSessionAndRemove()).Returns(() => TestFederation.AuthnRequest());
        _securityContext.Setup(x => x.SetLogoutRequestIdInSession(It.IsAny<string>())).Callback<string>(id => _sentLogoutRequestId = id);
        _securityContext.Setup(x => x.GetLogoutRequestIdFromSessionAndRemove()).Returns("_req1");
        _securityContext.Setup(x => x.GetCurrentPrincipal()).Returns(principal);
        _securityContext.Setup(x => x.GetLoggedInSubject()).Returns(new NameID { Value = _logoutMessage.NameId, Format = Saml20Constants.NameIdentifierFormats.Persistent });
        _securityContext.Setup(x => x.GetSessionIndex()).Returns("session-1");
        _securityContext.Setup(x => x.SignIn(It.IsAny<ClaimsPrincipal>())).Callback<ClaimsPrincipal>(p => _signedIn = p).Returns(Task.CompletedTask);
        _securityContext.Setup(x => x.SignOut()).Returns(Task.CompletedTask);
    }

    private Saml20LoginService LoginService()
    {
        var metadata = TestFederation.MetadataService(TestCertificates.Idp);
        var certificates = TestFederation.CertificateStore(TestCertificates.Idp);
        return new(new SamlDecryptionService(_options),
            new Saml20AuthnRequestBuilder(_options, metadata, [], TimeProvider.System),
            new HttpRedirectRequestBuilder(_options),
            new SamlAuthnResponseValidator(new SamlAssertionValidator(_options, TimeProvider.System), _options, certificates),
            _securityContext.Object, new InMemoryAssertionReplayCache(TimeProvider.System), _options, TestFederation.AuditLog(_log));
    }

    private Saml20LogoutService LogoutService()
    {
        var certificates = TestFederation.CertificateStore(TestCertificates.Idp);
        return new(_options, TestFederation.MetadataService(TestCertificates.Idp),
            new HttpRedirectRequestBuilder(_options), new HttpPostRequestBuilder(_options),
            new SamlLogoutResponseValidator(_options, certificates),
            new SamlLogoutRequestValidator(_options, certificates, TimeProvider.System),
            _securityContext.Object, TestFederation.AuditLog(_log), TimeProvider.System);
    }

    private async Task<FakeLogRecord> Login(SamlResponseBuilder response)
    {
        await LoginService().ProcessAuthenticationResponse(new TestLoginResponse(response.BuildBase64()));
        return _log.LatestRecord;
    }

    private async Task<FakeLogRecord> RejectedLogin(SamlResponseBuilder response)
    {
        await Assert.ThrowsAsync<Saml20ValidationException>(() => LoginService().ProcessAuthenticationResponse(new TestLoginResponse(response.BuildBase64())));
        return _log.LatestRecord;
    }

    private static IEnumerable<string?> Fields(FakeLogRecord record, params string[] names) =>
        names.Select(n => record.StructuredState!.FirstOrDefault(kv => kv.Key == n).Value);

    [Fact]
    public void SentAuthnRequest_IsLoggedWithItsId()
    {
        LoginService().GetSignedAuthnRequestString(new TestAuthnRequestParameters());

        Assert.Equal([_sentAuthnRequest!.ID], Fields(_log.LatestRecord, "AuthnRequestId"));
    }

    [Fact]
    public async Task AcceptedLogin_LogsWhatThePolicyRequires()
    {
        var response = new SamlResponseBuilder { ExtraAttributes = { [OioSaml4Constants.AttributeName_Privileges] = Privileges } };

        var record = await Login(response);

        Assert.Equal([response.ResponseId, TestFederation.AuthnRequestId, response.AssertionId, TestFederation.IdpEntityId, response.NameId, LoAValues.Substantial, Privileges],
            Fields(record, "ResponseId", "InResponseTo", "AssertionId", "Issuer", "NameId", "LevelOfAssurance", "Privileges"));
    }

    [Fact]
    public async Task AcceptedLogin_LogsTheLocalSessionTheUserIsSignedInWith()
    {
        var record = await Login(new SamlResponseBuilder());

        Assert.Equal([_signedIn!.FindFirst(ClaimsConstants.LocalSessionId)!.Value], Fields(record, "LocalSessionId"));
    }

    [Fact]
    public async Task RejectedLogin_LogsTheReason()
    {
        var response = new SamlResponseBuilder { Audience = "https://other.test" };

        var record = await RejectedLogin(response);

        Assert.Equal([response.ResponseId, TestFederation.AuthnRequestId, record.Exception!.Message], Fields(record, "ResponseId", "InResponseTo", "Reason"));
    }

    [Fact]
    public async Task RejectedLogin_IsAWarning()
    {
        var record = await RejectedLogin(new SamlResponseBuilder { SigningCertificate = TestCertificates.Sp });

        Assert.Equal(LogLevel.Warning, record.Level);
    }

    [Fact]
    public async Task SpInitiatedLogout_LogsWhatThePolicyRequires()
    {
        await LogoutService().ProcessSpInitiatedLogout(new TestLogoutParameters());

        Assert.Equal([_sentLogoutRequestId, _logoutMessage.NameId, "session-1", "local-1"],
            Fields(_log.LatestRecord, "LogoutRequestId", "NameId", "SessionIndex", "LocalSessionId"));
    }

    [Fact]
    public async Task LogoutResponse_LogsItsStatus()
    {
        var response = new LogoutMessageBuilder { InResponseTo = "_req1" };

        await LogoutService().ProcessIdpLogoutResponse(response.RedirectResponse());

        Assert.Equal([response.Id, "_req1", Saml20Constants.StatusCodes.Success], Fields(_log.LatestRecord, "LogoutResponseId", "InResponseTo", "Status"));
    }

    [Fact]
    public async Task FailedLogoutResponse_LogsItsStatus()
    {
        var response = new LogoutMessageBuilder { InResponseTo = "_req1", StatusCode = Saml20Constants.StatusCodes.Responder };

        await Assert.ThrowsAnyAsync<Saml20ValidationException>(() => LogoutService().ProcessIdpLogoutResponse(response.RedirectResponse()));

        Assert.Equal([Saml20Constants.StatusCodes.Responder], Fields(_log.LatestRecord, "Status"));
    }

    [Fact]
    public async Task IdpInitiatedLogout_LogsTheTerminatedSession()
    {
        await LogoutService().ProcessIdpInitiatedLogout(_logoutMessage.RedirectRequest());

        Assert.Equal([_logoutMessage.Id, TestFederation.IdpEntityId, _logoutMessage.NameId, "local-1"],
            Fields(_log.LatestRecord, "LogoutRequestId", "Issuer", "NameId", "LocalSessionId"));
    }

    [Fact]
    public async Task IdpInitiatedLogoutWithoutLocalSession_LogsThatThereWasNone()
    {
        _securityContext.Setup(x => x.GetCurrentPrincipal()).Returns((ClaimsPrincipal?)null);
        _securityContext.Setup(x => x.GetLoggedInSubject()).Returns((NameID?)null);

        await LogoutService().ProcessIdpInitiatedLogout(_logoutMessage.RedirectRequest());

        Assert.Equal(new EventId(7, nameof(AuditLog.LogoutRequestAcceptedWithoutSession)), _log.LatestRecord.Id);
    }

    [Fact]
    public async Task IdpInitiatedLogoutForAnotherUser_LogsTheRejection()
    {
        var request = new LogoutMessageBuilder { NameId = "https://data.gov.dk/model/core/eid/person/uuid/mallory" };

        await LogoutService().ProcessIdpInitiatedLogout(request.RedirectRequest());

        Assert.Equal(new EventId(8, nameof(AuditLog.LogoutRequestRejected)), _log.LatestRecord.Id);
    }

    [Fact]
    public async Task AuditEvents_AreLoggedUnderTheAuditCategory()
    {
        await Login(new SamlResponseBuilder());
        await LogoutService().ProcessSpInitiatedLogout(new TestLogoutParameters());

        Assert.All(_log.GetSnapshot(), r => Assert.Equal(AuditLog.Category, r.Category));
    }
}
