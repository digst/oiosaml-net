using System.Security.Claims;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Validation;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class SpInitiatedLogoutTests
{
    private readonly Mock<ISecurityContext> _securityContext = new();

    public SpInitiatedLogoutTests()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimsConstants.Issuer, TestFederation.IdpEntityId)], "test"));
        _securityContext.Setup(x => x.GetCurrentPrincipal()).Returns(principal);
        _securityContext.Setup(x => x.GetLoggedInSubject()).Returns(new NameID { Value = "alice", Format = Saml20Constants.NameIdentifierFormats.Persistent });
        _securityContext.Setup(x => x.GetSessionIndex()).Returns("session-1");
    }

    private Task<SignedLogoutRequest> Logout()
    {
        var options = TestFederation.Options();
        var certificates = TestFederation.CertificateStore(TestCertificates.Idp);
        return new Saml20LogoutService(options, TestFederation.MetadataService(TestCertificates.Idp),
            new HttpRedirectRequestBuilder(options), new HttpPostRequestBuilder(options),
            new SamlLogoutResponseValidator(options, certificates),
            new SamlLogoutRequestValidator(options, certificates, TimeProvider.System),
            _securityContext.Object, TestFederation.AuditLog(), TimeProvider.System).ProcessSpInitiatedLogout(new TestLogoutParameters());
    }

    private Task<string?> ProcessResponse(LogoutMessageBuilder response)
    {
        _securityContext.Setup(x => x.GetLogoutRequestIdFromSessionAndRemove()).Returns(response.InResponseTo);
        var options = TestFederation.Options();
        var certificates = TestFederation.CertificateStore(TestCertificates.Idp);
        return new Saml20LogoutService(options, TestFederation.MetadataService(TestCertificates.Idp),
            new HttpRedirectRequestBuilder(options), new HttpPostRequestBuilder(options),
            new SamlLogoutResponseValidator(options, certificates),
            new SamlLogoutRequestValidator(options, certificates, TimeProvider.System),
            _securityContext.Object, TestFederation.AuditLog(), TimeProvider.System).ProcessIdpLogoutResponse(response.RedirectResponse());
    }

    [Fact]
    public async Task LogoutResponseWithErrorStatus_IsReportedWithItsCodes()
    {
        var response = new LogoutMessageBuilder { InResponseTo = "_req1", StatusCode = Saml20Constants.StatusCodes.Responder, SubStatusCode = Saml20Constants.StatusCodes.RequestDenied };

        var e = await Assert.ThrowsAsync<Saml20StatusException>(() => ProcessResponse(response));

        Assert.Equal((Saml20Constants.StatusCodes.Responder, Saml20Constants.StatusCodes.RequestDenied), (e.StatusCode, e.SubStatusCode));
    }

    [Fact]
    public async Task UserWithoutSessionIndex_IsNotLoggedOutAtTheIdp()
    {
        _securityContext.Setup(x => x.GetSessionIndex()).Returns((string?)null);

        await Assert.ThrowsAsync<Saml20Exception>(Logout);
    }

    [Fact]
    public async Task UserWithoutNameId_IsNotLoggedOutAtTheIdp()
    {
        _securityContext.Setup(x => x.GetLoggedInSubject()).Returns(new NameID());

        await Assert.ThrowsAsync<Saml20Exception>(Logout);
    }

    [Fact]
    public async Task RefusedLogout_KeepsTheLocalSession()
    {
        _securityContext.Setup(x => x.GetSessionIndex()).Returns((string?)null);

        await Record.ExceptionAsync(Logout);

        _securityContext.Verify(x => x.SignOut(), Times.Never);
    }
}
