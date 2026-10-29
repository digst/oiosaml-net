using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Xml;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Validation;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class LoginFlowTests
{
    private readonly Mock<ISecurityContext> _securityContext = new();
    private readonly OioSamlOptions _options = TestFederation.Options();
    private readonly IAssertionReplayCache _replayCache = new InMemoryAssertionReplayCache(TimeProvider.System);
    private AuthnRequest? _authnRequest = TestFederation.AuthnRequest();
    private IIdentityProviderCertificateStore _certificateStore = TestFederation.CertificateStore(TestCertificates.Idp);
    private ClaimsPrincipal? _signedIn;

    private Saml20LoginService CreateService()
    {
        _securityContext.Setup(x => x.GetAuthnRequestFromSessionAndRemove()).Returns(() => _authnRequest);
        _securityContext.Setup(x => x.SignIn(It.IsAny<ClaimsPrincipal>())).Callback<ClaimsPrincipal>(p => _signedIn = p).Returns(Task.CompletedTask);
        var assertionValidator = new SamlAssertionValidator(_options, TimeProvider.System);
        var responseValidator = new SamlAuthnResponseValidator(assertionValidator, _options, _certificateStore);
        return new Saml20LoginService(new SamlDecryptionService(_options), null!, null!, responseValidator, _securityContext.Object, _replayCache, _options, TestFederation.AuditLog());
    }

    private Task Process(SamlResponseBuilder response) => CreateService().ProcessAuthenticationResponse(new TestLoginResponse(response.BuildBase64()));

    private async Task<string> Rejected(SamlResponseBuilder response)
    {
        var e = await Assert.ThrowsAnyAsync<Saml20ValidationException>(() => Process(response));
        Assert.Null(_signedIn);
        return e.Message;
    }

    [Fact]
    public async Task ValidResponse_SignsInTheSubject()
    {
        var response = new SamlResponseBuilder();

        await Process(response);

        Assert.Equal(response.NameId, _signedIn!.Identity!.Name);
        Assert.Equal(TestFederation.IdpEntityId, _signedIn.FindFirst(ClaimsConstants.Issuer)!.Value);
        Assert.Equal("session-1", _signedIn.FindFirst(ClaimsConstants.SessionIndex)!.Value);
    }

    [Fact]
    public async Task SignedResponseEnvelope_IsAccepted()
    {
        await Process(new SamlResponseBuilder { SignResponse = true });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task AssertionEncryptedForAnotherCertificate_IsRejected() =>
        Assert.Contains("probably encrypted for a different certificate",
            (await Assert.ThrowsAsync<Saml20Exception>(() => Process(new SamlResponseBuilder { EncryptionCertificate = TestCertificates.Other }))).Message);

    [Fact]
    public async Task UnsignedAssertion_IsRejected() =>
        Assert.Contains("[OIO-IDP-12]", await Rejected(new SamlResponseBuilder { SignAssertion = false }));

    [Fact]
    public async Task AssertionSignedByUntrustedKey_IsRejected() =>
        Assert.Contains("[OIO-IDP-12]", await Rejected(new SamlResponseBuilder { SigningCertificate = TestCertificates.Other }));

    [Fact]
    public async Task AssertionSignedWithARevokedIdpCertificate_IsRejected()
    {
        using var ca = new TestCertificateAuthority();
        var idpCertificate = ca.Issue();
        ca.Revoke(idpCertificate);
        _certificateStore = new IdentityProviderCertificateStore(_options, TestFederation.MetadataService(idpCertificate),
            new RevocationCertificateValidator(_options, TimeProvider.System), TimeProvider.System);

        Assert.Contains("Revoked", await Rejected(new SamlResponseBuilder { SigningCertificate = idpCertificate }));
    }

    [Fact]
    public async Task WrappedAssertion_IsRejected() =>
        Assert.Contains("[OIO-IDP-12]", await Rejected(new SamlResponseBuilder { AfterSigningAssertion = a => a.WrapInForgedAssertion("mallory") }));

    [Fact]
    public async Task TamperedResponseEnvelope_IsRejected()
    {
        var response = new SamlResponseBuilder { SignResponse = true, AfterSigningResponse = doc => doc.DocumentElement!.SetAttribute("Consent", "forged") };

        Assert.Contains("Invalid Response signature", await Rejected(response));
    }

    [Fact]
    public async Task Pkcs1KeyTransport_IsRejected()
    {
        var e = await Assert.ThrowsAsync<Saml20FormatException>(() => Process(new SamlResponseBuilder { KeyTransportAlgorithm = EncryptedXml.XmlEncRSA15Url }));

        Assert.Contains("RSA-OAEP is required", e.Message);
    }

    [Fact]
    public async Task BlockEncryptionOutsideAllowList_IsRejected()
    {
        var e = await Assert.ThrowsAsync<Saml20FormatException>(() => Process(new SamlResponseBuilder { DeclaredDataAlgorithm = EncryptedXml.XmlEncTripleDESUrl }));

        Assert.Contains("[OIO-ALG-01]", e.Message);
    }

    [Theory]
    [InlineData(AllowedAlgorithms.Aes128Gcm)]
    [InlineData(AllowedAlgorithms.Aes192Gcm)]
    [InlineData(AllowedAlgorithms.Aes256Gcm)]
    public async Task AssertionEncryptedWithAnAllowedBlockCipher_SignsIn(string algorithm)
    {
        await Process(new SamlResponseBuilder { DeclaredDataAlgorithm = algorithm });

        Assert.NotNull(_signedIn);
    }

    [Theory]
    [InlineData(EncryptedXml.XmlEncAES128Url)]
    [InlineData(EncryptedXml.XmlEncAES256Url)]
    public async Task CbcBlockEncryption_IsRejected(string algorithm) =>
        Assert.Contains("AES-GCM is required", (await Assert.ThrowsAsync<Saml20FormatException>(() => Process(new SamlResponseBuilder { DeclaredDataAlgorithm = algorithm }))).Message);

    [Fact]
    public async Task AlteredGcmCiphertext_IsRejected() =>
        Assert.Contains("altered", (await Assert.ThrowsAsync<Saml20FormatException>(() =>
            Process(new SamlResponseBuilder { DeclaredDataAlgorithm = AllowedAlgorithms.Aes256Gcm, TamperCipherValue = true }))).Message);

    [Fact]
    public async Task SessionKeyShorterThanTheBlockCipherNeeds_IsRejected() =>
        Assert.Contains("[OIO-ALG-01]", (await Assert.ThrowsAsync<Saml20FormatException>(() =>
            Process(new SamlResponseBuilder { DeclaredDataAlgorithm = AllowedAlgorithms.Aes256Gcm, SessionKeyBytes = 16 }))).Message);

    [Fact]
    public async Task Xmlenc11OaepWithSha256DigestAndMgf_SignsIn()
    {
        await Process(new SamlResponseBuilder
        {
            DeclaredDataAlgorithm = AllowedAlgorithms.Aes256Gcm,
            KeyTransportAlgorithm = AllowedAlgorithms.RsaOaep,
            OaepDigest = SignedXml.XmlDsigSHA256Url,
            OaepMgf = AllowedAlgorithms.Xmlenc11 + "mgf1sha256",
        });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task OaepWithDifferentDigestAndMgfHashes_IsReportedAsUnsupported() =>
        Assert.Contains("MGF1 SHA1", (await Assert.ThrowsAsync<Saml20FormatException>(() =>
            Process(new SamlResponseBuilder { KeyTransportAlgorithm = AllowedAlgorithms.RsaOaep, OaepDigest = SignedXml.XmlDsigSHA256Url }))).Message);

    [Fact]
    public async Task UnsolicitedResponse_IsRefusedBeforeDecryption()
    {
        _authnRequest = null;

        Assert.Contains("No pending AuthnRequest", await Rejected(new SamlResponseBuilder { TamperCipherValue = true }));
    }

    [Fact]
    public async Task MisdirectedResponse_IsRefusedBeforeDecryption() =>
        Assert.Contains("Response Destination must be", await Rejected(new SamlResponseBuilder { Destination = "https://other.test/acs", TamperCipherValue = true }));

    [Fact]
    public async Task DeeplyNestedResponse_IsAFormatError() =>
        Assert.Contains("nested more than", (await Assert.ThrowsAsync<Saml20FormatException>(() => Process(new SamlResponseBuilder { RawResponseXml = TestFederation.DeeplyNested() }))).InnerException!.Message);

    [Fact]
    public async Task ResponseWithDocumentTypeDefinition_IsRejected()
    {
        var response = new SamlResponseBuilder();
        response.RawResponseXml = """<!DOCTYPE x [<!ENTITY e "x">]>""" + response.Build().OuterXml;

        await Assert.ThrowsAsync<Saml20FormatException>(() => Process(response));
    }

    [Fact]
    public async Task ResponseNotBase64_IsRejected() =>
        await Assert.ThrowsAsync<Saml20FormatException>(() => CreateService().ProcessAuthenticationResponse(new TestLoginResponse("not base64!")));

    [Fact]
    public async Task NoPendingAuthnRequest_IsRejected()
    {
        _authnRequest = null;

        Assert.Contains("No pending AuthnRequest", await Rejected(new SamlResponseBuilder()));
    }

    [Fact]
    public async Task InResponseToMismatch_IsRejected() =>
        Assert.Contains("does not match the pending AuthnRequest", await Rejected(new SamlResponseBuilder { InResponseTo = "id-unknown-request-id" }));

    [Fact]
    public async Task SubjectConfirmationInResponseToMismatch_IsRejected() =>
        Assert.Contains("SubjectConfirmationData InResponseTo", await Rejected(new SamlResponseBuilder { SubjectConfirmationInResponseTo = "id-other" }));

    [Fact]
    public async Task WrongDestination_IsRejected() =>
        Assert.Contains("Destination", await Rejected(new SamlResponseBuilder { Destination = "https://evil.test/acs" }));

    [Fact]
    public async Task WrongRecipient_IsRejected() =>
        Assert.Contains("[OIO-IDP-17] SubjectConfirmationData Recipient", await Rejected(new SamlResponseBuilder { Recipient = "https://evil.test/acs" }));

    [Fact]
    public async Task WrongAudience_IsRejected() =>
        Assert.Contains("[OIO-IDP-18]", await Rejected(new SamlResponseBuilder { Audience = "https://other-sp.test" }));

    [Fact]
    public async Task UnknownIssuer_IsRejected() =>
        Assert.Contains("is not the configured identity provider", await Rejected(new SamlResponseBuilder { Issuer = "https://evil.test", ResponseIssuer = "https://evil.test" }));

    [Fact]
    public async Task ResponseIssuerDifferentFromAssertionIssuer_IsRejected() =>
        Assert.Contains("Response Issuer does not match", await Rejected(new SamlResponseBuilder { ResponseIssuer = "https://other.test" }));

    [Fact]
    public async Task IssuerFormatOtherThanEntity_IsRejected() =>
        Assert.Contains("[OIO-IDP-14]", await Rejected(new SamlResponseBuilder { IssuerFormat = Saml20Constants.NameIdentifierFormats.Unspecified }));

    [Fact]
    public async Task ErrorStatus_IsRejected() =>
        Assert.Contains("ErrorCode", await Rejected(new SamlResponseBuilder { StatusCode = Saml20Constants.StatusCodes.Responder }));

    [Fact]
    public async Task ErrorStatus_IsReportedWithItsCodes()
    {
        var response = new SamlResponseBuilder { StatusCode = Saml20Constants.StatusCodes.Responder, SubStatusCode = Saml20Constants.StatusCodes.AuthnFailed, StatusMessage = "Not authenticated" };

        var e = await Assert.ThrowsAsync<Saml20StatusException>(() => Process(response));

        Assert.Equal((Saml20Constants.StatusCodes.Responder, Saml20Constants.StatusCodes.AuthnFailed, "Not authenticated"), (e.StatusCode, e.SubStatusCode, e.StatusMessage));
    }

    [Fact]
    public async Task NoPassiveStatus_IsReportedWithItsCodes()
    {
        var response = new SamlResponseBuilder { StatusCode = Saml20Constants.StatusCodes.Responder, SubStatusCode = Saml20Constants.StatusCodes.NoPassive };

        Assert.Equal(Saml20Constants.StatusCodes.NoPassive, (await Assert.ThrowsAsync<Saml20StatusException>(() => Process(response))).SubStatusCode);
    }

    [Fact]
    public async Task OtherValidationFailures_AreNotStatusExceptions() =>
        Assert.IsNotType<Saml20StatusException>(await Assert.ThrowsAsync<Saml20ValidationException>(() => Process(new SamlResponseBuilder { SignAssertion = false })));

    [Fact]
    public async Task PlaintextAssertion_IsRejected() =>
        Assert.Contains("[OIO-IDP-13]", await Rejected(new SamlResponseBuilder { Encrypt = false }));

    [Fact]
    public async Task TwoEncryptedAssertions_AreRejected()
    {
        var response = new SamlResponseBuilder
        {
            BeforeSigningResponse = doc => doc.DocumentElement!.AppendChild(doc.GetElementsByTagName("EncryptedAssertion", Saml20Constants.ASSERTION)[0]!.CloneNode(true)),
        };

        Assert.Contains("[OIO-IDP-11]", await Rejected(response));
    }

    [Fact]
    public async Task EncryptedNameId_IsRejected()
    {
        var response = new SamlResponseBuilder { BeforeSigningAssertion = doc => Replace(doc, "NameID", $"""<saml:EncryptedID xmlns:saml="{Saml20Constants.ASSERTION}"><xenc:EncryptedData xmlns:xenc="{Saml20Constants.XENC}"/></saml:EncryptedID>""") };

        Assert.Contains("[OIO-IDP-13]", await Rejected(response));
    }

    [Fact]
    public async Task AttributeWithoutUriNameFormat_IsRejected() =>
        Assert.Contains("[OIO-AP-03]", await Rejected(new SamlResponseBuilder { AttributeNameFormat = "urn:oasis:names:tc:SAML:2.0:attrname-format:basic" }));

    [Fact]
    public async Task MissingAttributeStatement_IsRejected() =>
        Assert.Contains("[OIO-IDP-11]", await Rejected(new SamlResponseBuilder { BeforeSigningAssertion = doc => Remove(doc, "AttributeStatement") }));

    [Fact]
    public async Task MissingSubject_IsAValidationFailureNotACrash() =>
        Assert.Contains("[OIO-IDP-15]", await Rejected(new SamlResponseBuilder { BeforeSigningAssertion = doc => Remove(doc, "Subject") }));

    [Fact]
    public async Task MissingSubjectConfirmationData_IsAValidationFailureNotACrash() =>
        Assert.Contains("[OIO-IDP-17]", await Rejected(new SamlResponseBuilder { BeforeSigningAssertion = doc => Remove(doc, "SubjectConfirmationData") }));

    [Fact]
    public async Task MissingConditions_IsAValidationFailureNotACrash() =>
        Assert.Contains("[OIO-IDP-18]", await Rejected(new SamlResponseBuilder { BeforeSigningAssertion = doc => Remove(doc, "Conditions") }));

    [Fact]
    public async Task ExpiredAssertion_IsRejected() =>
        Assert.Contains("[OIO-GE-01]", await Rejected(new SamlResponseBuilder { Now = DateTime.UtcNow.AddMinutes(-30) }));

    [Fact]
    public async Task ClockSkewWithinLimit_IsAccepted()
    {
        await Process(new SamlResponseBuilder { Now = DateTime.UtcNow.AddMinutes(-7) });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task LoaBelowMinimum_IsRejected() =>
        Assert.Contains("LoA is too low", await Rejected(new SamlResponseBuilder { Loa = LoAValues.Low }));

    [Fact]
    public async Task RequestedLoaBelowTheConfiguredMinimum_DoesNotLowerIt()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml4Constants.GenericLoAPrefix + "/" + LoAValues.Low);

        Assert.Contains("[OIO-SP-16] LoA is too low. Expected minimum: Substantial", await Rejected(new SamlResponseBuilder { Loa = LoAValues.Low }));
    }

    [Fact]
    public async Task NsisLoaMeetingTheRequestedNsisLoa_SignsIn()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml4Constants.NsisLoAPrefix + "/" + LoAValues.High);

        await Process(new SamlResponseBuilder { LoaAttributeName = OioSaml4Constants.AttributeName_NsisLoA, Loa = LoAValues.High });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task NsisLoaBelowTheRequestedNsisLoa_IsRejected()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml4Constants.NsisLoAPrefix + "/" + LoAValues.High);

        Assert.Contains("LoA is too low", await Rejected(new SamlResponseBuilder { LoaAttributeName = OioSaml4Constants.AttributeName_NsisLoA, Loa = LoAValues.Substantial }));
    }

    [Fact]
    public async Task RequestedGenericLoa_IsCheckedAgainstTheNsisLoa()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml4Constants.GenericLoAPrefix + "/" + LoAValues.High);

        Assert.Contains("[OIO-SP-16] LoA is too low", await Rejected(new SamlResponseBuilder { Loa = LoAValues.Substantial }));
    }

    [Fact]
    public async Task RequestedGenericLoa_IsMetWithoutAGenericLoaAttribute()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml4Constants.GenericLoAPrefix + "/" + LoAValues.Substantial);

        await Process(new SamlResponseBuilder { Loa = LoAValues.Substantial });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task GenericLoaAttributeAlone_IsNotEnough() =>
        Assert.Contains("[OIO-SP-16] The assertion carries neither", await Rejected(new SamlResponseBuilder { LoaAttributeName = OioSaml4Constants.AttributeName_GenericLoA }));

    [Fact]
    public async Task EidasLoaMeetingTheMinimum_SignsIn()
    {
        await Process(new SamlResponseBuilder { LoaAttributeName = OioSaml4Constants.AttributeName_EidasLoA, Loa = LoAValues.EidasPrefix + "substantial" });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task EidasLoaBelowTheMinimum_IsRejected() =>
        Assert.Contains("LoA is too low", await Rejected(new SamlResponseBuilder { LoaAttributeName = OioSaml4Constants.AttributeName_EidasLoA, Loa = LoAValues.EidasPrefix + "low" }));

    [Fact]
    public async Task MinimumLoa_AppliesToNsisLoa()
    {
        _options.LevelOfAssurance.MinimumLoa = LoAValues.High;

        Assert.Contains("LoA is too low", await Rejected(new SamlResponseBuilder { Loa = LoAValues.Substantial }));
    }

    [Fact]
    public async Task MinimumLoa_AppliesToEidasLoa()
    {
        _options.LevelOfAssurance.MinimumLoa = LoAValues.High;

        Assert.Contains("LoA is too low", await Rejected(new SamlResponseBuilder { LoaAttributeName = OioSaml4Constants.AttributeName_EidasLoA, Loa = LoAValues.EidasPrefix + "substantial" }));
    }

    [Fact]
    public async Task NsisOverride_TakesPrecedenceOverMinimumLoa()
    {
        _options.LevelOfAssurance.MinimumLoa = LoAValues.High;
        _options.LevelOfAssurance.MinimumNsisLoa = LoAValues.Low;

        await Process(new SamlResponseBuilder { Loa = LoAValues.Low });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task WithoutRequestedProfiles_NoProfileAttributeIsNeeded()
    {
        await Process(new SamlResponseBuilder());

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task RequestedProfile_IsAccepted()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedProfiles: [OioSaml4AttributeProfiles.DkPerson, OioSaml4AttributeProfiles.DkProfessional]);
        var response = new SamlResponseBuilder();
        response.ExtraAttributes[OioSaml4Constants.AttributeName_AttributeProfile] = OioSaml4AttributeProfiles.DkProfessional;

        await Process(response);

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task ProfileThatWasNotRequested_IsRejected()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedProfiles: [OioSaml4AttributeProfiles.DkPerson]);
        var response = new SamlResponseBuilder();
        response.ExtraAttributes[OioSaml4Constants.AttributeName_AttributeProfile] = OioSaml4AttributeProfiles.DkPersonAnonymous;

        Assert.Contains("[OIO-SP-07]", await Rejected(response));
    }

    [Fact]
    public async Task RequestedProfilesWithoutAProfileAttribute_IsRejected()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedProfiles: [OioSaml4AttributeProfiles.DkPerson]);

        Assert.Contains(OioSaml4Constants.AttributeName_AttributeProfile, await Rejected(new SamlResponseBuilder()));
    }

    [Fact]
    public async Task RequestedOioSaml3Profile_IsAccepted()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml3Profiles.AuthnContextClassRefPrefix + OioSaml3Profiles.Professional);

        await Process(new SamlResponseBuilder { NameId = "https://data.gov.dk/model/core/eid/professional/uuid/bob" });

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task OioSaml3ProfileOtherThanRequested_IsRejected()
    {
        _authnRequest = TestFederation.AuthnRequest(requestedClassRef: OioSaml3Profiles.AuthnContextClassRefPrefix + OioSaml3Profiles.Professional);

        Assert.Contains("asked for a Professional identity", await Rejected(new SamlResponseBuilder()));
    }

    [Fact]
    public async Task DkPersonWithNsisAndEidasLoa_IsCheckedOnTheNsisLoa()
    {
        var response = new SamlResponseBuilder { Loa = LoAValues.Substantial };
        response.ExtraAttributes[OioSaml4Constants.AttributeName_EidasLoA] = LoAValues.EidasPrefix + "low";

        await Process(response);

        Assert.NotNull(_signedIn);
    }

    [Fact]
    public async Task StaleAuthnInstantAfterForceAuthn_IsRejected()
    {
        _authnRequest = TestFederation.AuthnRequest(forceAuthn: true);

        Assert.Contains("[OIO-SP-15]", await Rejected(new SamlResponseBuilder { AuthnInstant = DateTime.UtcNow.AddMinutes(-15) }));
    }

    [Fact]
    public async Task ReplayedAssertion_IsRejected()
    {
        var response = new SamlResponseBuilder();
        await Process(response);

        var e = await Assert.ThrowsAsync<Saml20ValidationException>(() => Process(response));
        Assert.Contains("already been used", e.Message);
    }

    private static void Remove(XmlDocument doc, string localName)
    {
        var element = doc.GetElementsByTagName(localName, Saml20Constants.ASSERTION)[0]!;
        element.ParentNode!.RemoveChild(element);
    }

    private static void Replace(XmlDocument doc, string localName, string xml)
    {
        var element = doc.GetElementsByTagName(localName, Saml20Constants.ASSERTION)[0]!;
        var fragment = doc.CreateDocumentFragment();
        fragment.InnerXml = xml;
        element.ParentNode!.ReplaceChild(fragment, element);
    }
}
