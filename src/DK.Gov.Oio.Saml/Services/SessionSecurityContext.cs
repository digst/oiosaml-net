using System.Security.Claims;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Services;

// What the library keeps in the host's session, and how; a host supplies the session store and the authentication cookie.
public abstract class SessionSecurityContext : ISecurityContext
{
    private const string AuthnRequestKey = "OioSaml-AuthnRequest";
    private const string LogoutRequestIdKey = "OioSaml-LogoutRequestId";
    private const string LoginRelayStateKey = "OioSaml-LoginRelayState";
    private const string LogoutRelayStateKey = "OioSaml-LogoutRelayState";
    private const string ReturnUrlKey = "OioSaml-ReturnUrl";

    public abstract ClaimsPrincipal? GetCurrentPrincipal();

    public abstract Task SignIn(ClaimsPrincipal claimsPrincipal);

    public abstract Task SignOut();

    protected abstract string? GetSessionValue(string key);

    protected abstract void SetSessionValue(string key, string value);

    protected abstract void RemoveSessionValue(string key);

    public string? GetSessionIndex() => GetCurrentPrincipal()?.GetClaimValue(ClaimsConstants.SessionIndex);

    public NameID? GetLoggedInSubject() => GetCurrentPrincipal() is { } principal
        ? new NameID { Value = principal.GetClaimValue(ClaimsConstants.SubjectNameId), Format = principal.GetClaimValue(ClaimsConstants.SubjectNameIdFormat) }
        : null;

    public void SetAuthnRequestInSession(AuthnRequest authnRequest) => SetSessionValue(AuthnRequestKey, Serialization.SerializeToXmlString(authnRequest));

    public AuthnRequest? GetAuthnRequestFromSessionAndRemove() =>
        TakeFromSession(AuthnRequestKey) is { } xml ? Serialization.DeserializeFromXmlString<AuthnRequest>(xml) : null;

    public void SetLogoutRequestIdInSession(string logoutRequestId) => SetSessionValue(LogoutRequestIdKey, logoutRequestId);

    public string? GetLogoutRequestIdFromSessionAndRemove() => TakeFromSession(LogoutRequestIdKey);

    public void SetLoginRelayStateInSession(PendingRelayState relayState) => SetRelayState(LoginRelayStateKey, relayState);

    public PendingRelayState? GetLoginRelayStateFromSessionAndRemove() => TakeRelayState(LoginRelayStateKey);

    public void SetLogoutRelayStateInSession(PendingRelayState relayState) => SetRelayState(LogoutRelayStateKey, relayState);

    public PendingRelayState? GetLogoutRelayStateFromSessionAndRemove() => TakeRelayState(LogoutRelayStateKey);

    public void SetReturnUrlInSession(string? returnUrl)
    {
        if (returnUrl == null) RemoveSessionValue(ReturnUrlKey);
        else SetSessionValue(ReturnUrlKey, returnUrl);
    }

    public string? GetReturnUrlFromSessionAndRemove() => TakeFromSession(ReturnUrlKey);

    private string? TakeFromSession(string key)
    {
        var value = GetSessionValue(key);
        if (value != null) RemoveSessionValue(key);
        return value;
    }

    private void SetRelayState(string key, PendingRelayState relayState) =>
        SetSessionValue(key, relayState.Value == null ? relayState.Token : relayState.Token + "\n" + relayState.Value);

    private PendingRelayState? TakeRelayState(string key) => TakeFromSession(key)?.Split(['\n'], 2) is { } parts
        ? new PendingRelayState(parts[0], parts.Length == 2 ? parts[1] : null)
        : null;
}
