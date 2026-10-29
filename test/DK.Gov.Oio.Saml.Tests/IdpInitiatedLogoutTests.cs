using System.Web;
using DK.Gov.Oio.Saml.Builders;
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

public class IdpInitiatedLogoutTests
{
    private readonly Mock<ISecurityContext> _securityContext = new();
    private readonly Saml20LogoutService _service;

    public IdpInitiatedLogoutTests()
    {
        var options = TestFederation.Options();
        var metadata = TestFederation.MetadataService(TestCertificates.Idp);
        _securityContext.Setup(x => x.GetLoggedInSubject()).Returns(new NameID { Value = new LogoutMessageBuilder().NameId, Format = Saml20Constants.NameIdentifierFormats.Persistent });
        _securityContext.Setup(x => x.SignOut()).Returns(Task.CompletedTask);
        _service = new Saml20LogoutService(options, metadata,
            new HttpRedirectRequestBuilder(options), new HttpPostRequestBuilder(options),
            new SamlLogoutResponseValidator(options, TestFederation.CertificateStore(TestCertificates.Idp)),
            new SamlLogoutRequestValidator(options, TestFederation.CertificateStore(TestCertificates.Idp), TimeProvider.System),
            _securityContext.Object, TestFederation.AuditLog(), TimeProvider.System);
    }

    [Fact]
    public async Task ValidRequest_SignsOutAndRespondsSuccess()
    {
        var request = new LogoutMessageBuilder();

        var response = await _service.ProcessIdpInitiatedLogout(request.RedirectRequest());

        _securityContext.Verify(x => x.SignOut(), Times.Once);
        var logoutResponse = DecodeRedirectResponse(response.Response);
        Assert.Equal(Saml20Constants.StatusCodes.Success, logoutResponse.Status.StatusCode.Value);
        Assert.Equal(request.Id, logoutResponse.InResponseTo);
        Assert.Equal(TestFederation.IdpSloRedirectUrl, logoutResponse.Destination);
    }

    [Fact]
    public async Task RequestForAnotherSubject_DoesNotSignOutAndRespondsRequestDenied()
    {
        var response = await _service.ProcessIdpInitiatedLogout(new LogoutMessageBuilder { NameId = "bob" }.RedirectRequest());

        _securityContext.Verify(x => x.SignOut(), Times.Never);
        Assert.Equal(Saml20Constants.StatusCodes.RequestDenied, DecodeRedirectResponse(response.Response).Status.StatusCode.Value);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(10)]
    public async Task RequestIssuedOutsideTheAllowedWindow_DoesNotSignOutAndRespondsRequestDenied(int minutes)
    {
        var response = await _service.ProcessIdpInitiatedLogout(new LogoutMessageBuilder { IssueInstant = DateTime.UtcNow.AddMinutes(minutes) }.RedirectRequest());

        _securityContext.Verify(x => x.SignOut(), Times.Never);
        Assert.Equal(Saml20Constants.StatusCodes.RequestDenied, DecodeRedirectResponse(response.Response).Status.StatusCode.Value);
    }

    [Fact]
    public async Task OversizedRedirectMessage_IsAFormatError()
    {
        var parameters = new LogoutMessageBuilder().RedirectRequest();
        using var compressed = new MemoryStream();
        using (var deflate = new System.IO.Compression.DeflateStream(compressed, System.IO.Compression.CompressionMode.Compress))
            deflate.Write(new byte[Extensions.Extensions.MaximumInflatedMessageLength + 1]);
        parameters.LogoutRequest = Convert.ToBase64String(compressed.ToArray());

        Assert.Contains("inflates to more than", (await Assert.ThrowsAsync<Saml20FormatException>(() => _service.ProcessIdpInitiatedLogout(parameters))).Message);
    }

    [Fact]
    public async Task DeeplyNestedPostedLogoutResponse_IsAFormatError()
    {
        var parameters = new LogoutMessageBuilder().PostRequest();
        parameters.LogoutRequest = null;
        parameters.LogoutResponse = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(TestFederation.DeeplyNested("LogoutResponse")));

        await Assert.ThrowsAsync<Saml20FormatException>(() => _service.ProcessIdpLogoutResponse(parameters));
    }

    [Fact]
    public async Task MessageThatIsNotBase64_IsAFormatError()
    {
        var parameters = new LogoutMessageBuilder().RedirectRequest();
        parameters.LogoutRequest = "not base64!";

        await Assert.ThrowsAsync<Saml20FormatException>(() => _service.ProcessIdpInitiatedLogout(parameters));
    }

    [Fact]
    public async Task RequestFromAnotherIssuer_GetsNoResponse() =>
        await Assert.ThrowsAsync<Saml20ValidationException>(() => _service.ProcessIdpInitiatedLogout(new LogoutMessageBuilder { Issuer = "https://evil.test" }.RedirectRequest()));

    [Fact]
    public async Task UnsignedRequest_DoesNotSignOut()
    {
        var response = await _service.ProcessIdpInitiatedLogout(new LogoutMessageBuilder { Sign = false }.PostRequest());

        _securityContext.Verify(x => x.SignOut(), Times.Never);
        Assert.False(response.IsRedirectResponse);
    }

    private static LogoutResponse DecodeRedirectResponse(string url)
    {
        var query = HttpUtility.ParseQueryString(url.Substring(url.IndexOf('?') + 1));
        var xml = query["SAMLResponse"]!.DeflateDecompress();
        return Serialization.DeserializeFromXmlString<LogoutResponse>(xml);
    }
}
