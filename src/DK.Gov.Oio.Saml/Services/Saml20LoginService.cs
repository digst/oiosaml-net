using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Logging;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Utils;
using DK.Gov.Oio.Saml.Validation;
using DK.Gov.Oio.Saml.Validation.Model;

namespace DK.Gov.Oio.Saml.Services;

internal class Saml20LoginService(
    ISamlDecryptionService decryptionService,
    Saml20AuthnRequestBuilder authnRequestBuilder,
    HttpRedirectRequestBuilder redirectRequestBuilder,
    SamlAuthnResponseValidator responseValidator,
    ISecurityContext securityContext,
    IAssertionReplayCache replayCache,
    OioSamlOptions options,
    AuditLog auditLog) : ISaml20LoginService
{
    public string GetSignedAuthnRequestString(IAuthnRequestParameters authnRequestParameters)
    {
        var authnRequest = authnRequestBuilder.CreateSamlAuthnRequest(authnRequestParameters);
        var relayState = PendingRelayState.Issue(authnRequestParameters.RelayState);
        securityContext.SetAuthnRequestInSession(authnRequest.Request);
        securityContext.SetLoginRelayStateInSession(relayState);
        securityContext.SetReturnUrlInSession(LocalUrl.IsLocal(authnRequestParameters.ReturnUrl) ? authnRequestParameters.ReturnUrl : null);
        var url = redirectRequestBuilder.GetSignedAuthnRequest(authnRequest, relayState.Token);
        auditLog.AuthnRequestSent(authnRequest.Request.ID, authnRequest.Request.Destination);
        return url;
    }

    public async Task<LoginResult> ProcessAuthenticationResponse(ISamlLoginResponse loginResponse)
    {
        XmlElement? response = null;
        try
        {
            var pendingRelayState = securityContext.GetLoginRelayStateFromSessionAndRemove();
            var returnUrl = securityContext.GetReturnUrlFromSessionAndRemove();
            response = ParseResponse(loginResponse.LoginResponse);
            var model = new SamlAuthnResponseValidationModel
            {
                Response = response,
                AuthnRequest = securityContext.GetAuthnRequestFromSessionAndRemove(),
            };
            var envelope = await responseValidator.ValidateBeforeDecryptionAsync(model);
            if (!envelope.IsValid) throw envelope.ToException();
            model.AssertionXml = decryptionService.Decrypt(response.GetSingleChild("EncryptedAssertion")!);
            model.Assertion = Serialization.Deserialize<Assertion>(new XmlNodeReader(model.AssertionXml));
            var result = await responseValidator.ValidateAsync(model);
            if (!result.IsValid) throw result.ToException();
            var relayState = PendingRelayState.Redeem(pendingRelayState, loginResponse.RelayState);

            var assertion = model.Assertion!;
            var expires = assertion.GetBearerConfirmationData()!.NotOnOrAfter!.Value.Add(options.AllowedClockSkew);
            if (!replayCache.TryAdd(assertion.ID, expires)) throw new Saml20ValidationException($"Assertion {assertion.ID} has already been used.");
            var localSessionId = Guid.NewGuid().ToString("N");
            await securityContext.SignIn(assertion.ToClaimsPrincipal(localSessionId));
            auditLog.LoginAccepted(response.GetAttribute("ID"), response.GetAttribute("InResponseTo"), assertion.ID, assertion.Issuer.Value,
                assertion.GetSubject().Value, LevelOfAssurance(assertion), string.Join(",", assertion.GetAttributeValues(OioSaml4Constants.AttributeName_Privileges)), localSessionId);
            return new LoginResult(returnUrl, relayState);
        }
        catch (Exception e)
        {
            auditLog.LoginRejected(response?.GetAttribute("ID"), response?.GetAttribute("InResponseTo"), e.Message, e);
            throw;
        }
    }

    private static string? LevelOfAssurance(Assertion assertion) =>
        assertion.GetAttributeValues(OioSaml4Constants.AttributeName_NsisLoA).Concat(assertion.GetAttributeValues(OioSaml4Constants.AttributeName_EidasLoA)).FirstOrDefault();

    private static XmlElement ParseResponse(string? base64)
    {
        try { return SecureXml.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64 ?? string.Empty))).DocumentElement!; }
        catch (Exception e) when (e is FormatException or XmlException) { throw new Saml20FormatException("SAMLResponse is not a base64 encoded XML document.", e); }
    }
}
