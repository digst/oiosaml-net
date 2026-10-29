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
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;
using DK.Gov.Oio.Saml.Validation;
using DK.Gov.Oio.Saml.Validation.Model;

namespace DK.Gov.Oio.Saml.Services;

internal class Saml20LogoutService(
    OioSamlOptions options,
    IIdentityProviderMetadataService metadataService,
    HttpRedirectRequestBuilder redirectRequestBuilder,
    HttpPostRequestBuilder postRequestBuilder,
    SamlLogoutResponseValidator logoutResponseValidator,
    SamlLogoutRequestValidator logoutRequestValidator,
    ISecurityContext securityContext,
    AuditLog auditLog,
    TimeProvider timeProvider) : ISaml20LogoutService
{
    public async Task<SignedLogoutRequest> ProcessSpInitiatedLogout(ILogoutParameters logoutParameters)
    {
        var principal = securityContext.GetCurrentPrincipal()
                        ?? throw new Saml20Exception("No authenticated user found.");
        var slo = metadataService.GetMetadata().SingleLogoutServices.FirstOrDefault()
                  ?? throw new Saml20Exception("IdP metadata has no SingleLogoutService.");
        var subject = securityContext.GetLoggedInSubject();
        var sessionIndex = securityContext.GetSessionIndex();
        if (subject?.Value == null || sessionIndex == null)
            throw new Saml20Exception("The signed-in user has no SAML NameID and SessionIndex, so no LogoutRequest can be sent.");
        var request = new Saml20LogoutRequest(timeProvider.GetUtcNow().UtcDateTime)
        {
            Issuer = options.ServiceProvider.EntityId,
            SubjectToLogOut = subject,
            Reason = Saml20Constants.Reasons.User,
            Destination = slo.Location,
            SessionIndex = sessionIndex,
        };
        var relayState = PendingRelayState.Issue(logoutParameters.RelayState);
        securityContext.SetLogoutRequestIdInSession(request.ID);
        securityContext.SetLogoutRelayStateInSession(relayState);
        await securityContext.SignOut(); // [OIO-SP-28] the local session ends before the request is sent
        auditLog.LogoutRequestSent(request.ID, slo.Location, request.SubjectToLogOut?.Value, request.SessionIndex, principal.GetClaimValue(ClaimsConstants.LocalSessionId));
        if (slo.IsRedirectBinding()) return redirectRequestBuilder.GetSignedLogoutRequest(request, relayState.Token);
        if (slo.IsPostBinding()) return postRequestBuilder.GetSignedLogoutRequest(request, relayState.Token);
        throw new Saml20Exception($"Unsupported SAML binding: {slo.Binding}");
    }

    public async Task<string?> ProcessIdpLogoutResponse(ILogoutParameters logoutParameters)
    {
        LogoutResponse? response = null;
        try
        {
            var pendingRelayState = securityContext.GetLogoutRelayStateFromSessionAndRemove();
            var doc = ParseMessage(logoutParameters);
            response = Serialization.Deserialize<LogoutResponse>(new XmlNodeReader(doc));
            var model = new LogoutResponseModel
            {
                RawXml = doc.DocumentElement!,
                Response = response,
                LogoutParameters = logoutParameters,
                ExpectedInResponseTo = securityContext.GetLogoutRequestIdFromSessionAndRemove(),
            };
            var result = await logoutResponseValidator.ValidateAsync(model);
            if (!result.IsValid) throw result.ToException();
            var relayState = PendingRelayState.Redeem(pendingRelayState, logoutParameters.RelayState);
            auditLog.LogoutResponseAccepted(response.ID, response.InResponseTo, response.Status.StatusCode.Value);
            return relayState;
        }
        catch (Exception e)
        {
            auditLog.LogoutResponseRejected(response?.ID, response?.InResponseTo, response?.Status?.StatusCode?.Value, e.Message, e);
            throw;
        }
    }

    public async Task<SignedLogoutResponse> ProcessIdpInitiatedLogout(ILogoutParameters logoutParameters)
    {
        LogoutRequest? request = null;
        try
        {
            var doc = ParseMessage(logoutParameters);
            request = Serialization.Deserialize<LogoutRequest>(new XmlNodeReader(doc));
            var model = new LogoutRequestModel
            {
                RawXml = doc.DocumentElement!,
                Request = request,
                LogoutParameters = logoutParameters,
                LoggedInSubject = securityContext.GetLoggedInSubject(),
            };
            var result = await logoutRequestValidator.ValidateAsync(model);
            // A request not from the configured IdP gets no answer at all, not even a RequestDenied.
            if (request.Issuer?.Value != options.IdentityProvider.EntityId)
                throw new Saml20ValidationException(result.GetValidationErrors());
            var slo = metadataService.GetMetadata().SingleLogoutServices.FirstOrDefault(x => x.Binding == logoutParameters.Binding)
                      ?? throw new Saml20Exception($"IdP has no SingleLogoutService for binding {logoutParameters.Binding}.");
            var response = new Saml20LogoutResponse(timeProvider.GetUtcNow().UtcDateTime)
            {
                Issuer = options.ServiceProvider.EntityId,
                InResponseTo = request.ID,
                Destination = slo.Location,
                StatusCode = result.IsValid ? Saml20Constants.StatusCodes.Success : Saml20Constants.StatusCodes.RequestDenied,
            };
            var relayState = result.IsValid ? logoutParameters.RelayState : null; // [SAMLBind 3.4.3] echoed unchanged
            var signedResponse = logoutParameters.Binding == Saml20Constants.ProtocolBindings.HTTP_Redirect
                ? redirectRequestBuilder.GetSignedLogoutResponse(response, relayState)
                : postRequestBuilder.GetSignedLogoutResponse(response, relayState);
            if (result.IsValid)
            {
                var localSessionId = securityContext.GetCurrentPrincipal()?.GetClaimValue(ClaimsConstants.LocalSessionId);
                await securityContext.SignOut();
                if (localSessionId != null)
                    auditLog.LogoutRequestAccepted(request.ID, options.IdentityProvider.EntityId, NameIdOf(request), localSessionId);
                else
                    auditLog.LogoutRequestAcceptedWithoutSession(request.ID, options.IdentityProvider.EntityId, NameIdOf(request));
            }
            else auditLog.LogoutRequestRejected(request.ID, options.IdentityProvider.EntityId, NameIdOf(request), result.GetValidationErrors());
            return signedResponse;
        }
        catch (Exception e)
        {
            auditLog.LogoutRequestRejected(request?.ID, request?.Issuer?.Value, NameIdOf(request), e.Message);
            throw;
        }
    }

    private static string? NameIdOf(LogoutRequest? request) => (request?.Item as NameID)?.Value;

    private static XmlDocument ParseMessage(ILogoutParameters p)
    {
        var message = string.IsNullOrEmpty(p.LogoutRequest) ? p.LogoutResponse : p.LogoutRequest;
        if (string.IsNullOrEmpty(message)) throw new Saml20Exception("No SAML message received.");
        try
        {
            return SecureXml.Parse(p.Binding == Saml20Constants.ProtocolBindings.HTTP_Redirect ? message!.DeflateDecompress() : Encoding.UTF8.GetString(Convert.FromBase64String(message!)));
        }
        catch (Exception e) when (e is FormatException or XmlException) { throw new Saml20FormatException("The SAML message is not a base64-encoded XML document.", e); }
    }
}
