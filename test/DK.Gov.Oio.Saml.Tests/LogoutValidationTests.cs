using System.Security.Cryptography.Xml;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Utils;
using DK.Gov.Oio.Saml.Validation;
using DK.Gov.Oio.Saml.Validation.Model;
using FluentValidation.Results;

namespace DK.Gov.Oio.Saml.Tests;

public class LogoutValidationTests
{
    private static readonly NameID Alice = new() { Value = "https://data.gov.dk/model/core/eid/person/uuid/alice", Format = Saml20Constants.NameIdentifierFormats.Persistent };
    private readonly SamlLogoutRequestValidator _requestValidator = new(TestFederation.Options(), TestFederation.CertificateStore(TestCertificates.Idp), TimeProvider.System);
    private readonly SamlLogoutResponseValidator _responseValidator = new(TestFederation.Options(), TestFederation.CertificateStore(TestCertificates.Idp));

    private Task<ValidationResult> ValidateRequest(TestLogoutParameters parameters, bool loggedIn = true)
    {
        var doc = SecureXml.Parse(Decode(parameters, parameters.LogoutRequest!));
        return _requestValidator.ValidateAsync(new LogoutRequestModel
        {
            RawXml = doc.DocumentElement!,
            Request = Serialization.Deserialize<LogoutRequest>(new XmlNodeReader(doc)),
            LogoutParameters = parameters,
            LoggedInSubject = loggedIn ? Alice : null,
        });
    }

    private Task<ValidationResult> ValidateResponse(TestLogoutParameters parameters, string? expectedInResponseTo)
    {
        var doc = SecureXml.Parse(Decode(parameters, parameters.LogoutResponse!));
        return _responseValidator.ValidateAsync(new LogoutResponseModel
        {
            RawXml = doc.DocumentElement!,
            Response = Serialization.Deserialize<LogoutResponse>(new XmlNodeReader(doc)),
            LogoutParameters = parameters,
            ExpectedInResponseTo = expectedInResponseTo,
        });
    }

    private static TestLogoutParameters WithRawQuery(TestLogoutParameters parameters, Func<string, string?> change)
    {
        parameters.RawQuery = change(parameters.RawQuery!);
        return parameters;
    }

    private static string Decode(ILogoutParameters parameters, string message) =>
        parameters.Binding == Saml20Constants.ProtocolBindings.HTTP_Redirect ? message.DeflateDecompress() : System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message));

    [Fact]
    public async Task SignedPostLogoutRequest_IsValid() => Assert.True((await ValidateRequest(new LogoutMessageBuilder().PostRequest())).IsValid);

    [Fact]
    public async Task SignedRedirectLogoutRequest_IsValid() => Assert.True((await ValidateRequest(new LogoutMessageBuilder().RedirectRequest())).IsValid);

    [Fact]
    public async Task RedirectSignedOverUpperCaseEscapes_IsValid() =>
        Assert.True((await ValidateRequest(new LogoutMessageBuilder { Encode = Uri.EscapeDataString, RelayState = "a b/c" }.RedirectRequest())).IsValid);

    [Fact]
    public async Task RedirectWithoutRawQuery_IsRejected() =>
        Assert.Contains("[OIO-IDP-23]", (await ValidateRequest(WithRawQuery(new LogoutMessageBuilder().RedirectRequest(), _ => null))).GetValidationErrors());

    [Fact]
    public async Task RedirectRepeatingTheMessage_IsRejected() =>
        Assert.Contains("[OIO-IDP-23]", (await ValidateRequest(WithRawQuery(new LogoutMessageBuilder().RedirectRequest(), q => "SAMLRequest=x&" + q))).GetValidationErrors());

    [Fact]
    public async Task RedirectRepeatingTheMessageUnderAnEncodedName_IsRejected() =>
        Assert.Contains("[OIO-IDP-23]", (await ValidateRequest(WithRawQuery(new LogoutMessageBuilder().RedirectRequest(), q => "saml%52equest=x&" + q))).GetValidationErrors());

    [Fact]
    public async Task RedirectWhoseBoundRelayStateWasNotSigned_IsRejected()
    {
        var parameters = new LogoutMessageBuilder().RedirectRequest();
        parameters.RelayState = "injected";

        Assert.Contains("[OIO-IDP-23]", (await ValidateRequest(parameters)).GetValidationErrors());
    }

    [Fact]
    public async Task UnsignedLogoutRequest_IsRejected() =>
        Assert.Contains("[OIO-IDP-23]", (await ValidateRequest(new LogoutMessageBuilder { Sign = false }.PostRequest())).GetValidationErrors());

    [Fact]
    public async Task RedirectLogoutRequestSignedByUntrustedKey_IsRejected() =>
        Assert.Contains("[OIO-IDP-23]", (await ValidateRequest(new LogoutMessageBuilder { SigningCertificate = TestCertificates.Other }.RedirectRequest())).GetValidationErrors());

    [Fact]
    public async Task RedirectSignatureAlgorithmOutsideAllowList_IsRejected() =>
        Assert.Contains("[OIO-ALG-01]", (await ValidateRequest(new LogoutMessageBuilder { SigAlg = SignedXml.XmlDsigRSASHA1Url }.RedirectRequest())).GetValidationErrors());

    [Fact]
    public async Task LogoutRequestFromUnknownIssuer_IsRejected() =>
        Assert.Contains("is not the configured identity provider", (await ValidateRequest(new LogoutMessageBuilder { Issuer = "https://evil.test" }.PostRequest())).GetValidationErrors());

    [Fact]
    public async Task LogoutRequestWithWrongDestination_IsRejected() =>
        Assert.Contains("Destination", (await ValidateRequest(new LogoutMessageBuilder { Destination = "https://evil.test/logout" }.PostRequest())).GetValidationErrors());

    [Fact]
    public async Task LogoutRequestForAnotherSubject_IsRejected() =>
        Assert.Contains("[OIO-SP-23]", (await ValidateRequest(new LogoutMessageBuilder { NameId = "bob" }.PostRequest())).GetValidationErrors());

    [Fact]
    public async Task LogoutRequestWithoutLocalSession_IsValid() =>
        Assert.True((await ValidateRequest(new LogoutMessageBuilder { NameId = "bob" }.PostRequest(), loggedIn: false)).IsValid);

    [Fact]
    public async Task LogoutRequestWithEncryptedNameId_IsRejected()
    {
        var builder = new LogoutMessageBuilder
        {
            BeforeSigning = doc =>
            {
                var nameId = doc.GetElementsByTagName("NameID", Saml20Constants.ASSERTION)[0]!;
                var fragment = doc.CreateDocumentFragment();
                fragment.InnerXml = $"""<saml:EncryptedID xmlns:saml="{Saml20Constants.ASSERTION}"><xenc:EncryptedData xmlns:xenc="{Saml20Constants.XENC}"/></saml:EncryptedID>""";
                nameId.ParentNode!.ReplaceChild(fragment, nameId);
            },
        };

        Assert.Contains("[OIO-IDP-24]", (await ValidateRequest(builder.PostRequest())).GetValidationErrors());
    }

    [Fact]
    public async Task SignedLogoutResponse_IsValid() =>
        Assert.True((await ValidateResponse(new LogoutMessageBuilder { InResponseTo = "_req1" }.PostResponse(), "_req1")).IsValid);

    [Fact]
    public async Task SignedRedirectLogoutResponse_IsValid() =>
        Assert.True((await ValidateResponse(new LogoutMessageBuilder { InResponseTo = "_req1" }.RedirectResponse(), "_req1")).IsValid);

    [Fact]
    public async Task LogoutResponseForUnknownRequest_IsRejected() =>
        Assert.Contains("InResponseTo does not match", (await ValidateResponse(new LogoutMessageBuilder { InResponseTo = "_req1" }.PostResponse(), "_other")).GetValidationErrors());

    [Fact]
    public async Task LogoutResponseWithoutPendingRequest_IsRejected() =>
        Assert.Contains("InResponseTo does not match", (await ValidateResponse(new LogoutMessageBuilder { InResponseTo = "_req1" }.PostResponse(), null)).GetValidationErrors());

    [Fact]
    public async Task LogoutResponseWithFailureStatus_IsRejected() =>
        Assert.Contains("[OIO-IDP-27]", (await ValidateResponse(new LogoutMessageBuilder { InResponseTo = "_req1", StatusCode = Saml20Constants.StatusCodes.Responder }.PostResponse(), "_req1")).GetValidationErrors());

    [Fact]
    public async Task UnsignedLogoutResponse_IsRejected() =>
        Assert.Contains("[OIO-IDP-26]", (await ValidateResponse(new LogoutMessageBuilder { InResponseTo = "_req1", Sign = false }.PostResponse(), "_req1")).GetValidationErrors());
}
