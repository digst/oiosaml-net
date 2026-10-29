using System.Security.Claims;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.Interfaces;

public interface ISecurityContext
{
    string? GetSessionIndex();
    NameID? GetLoggedInSubject();
    ClaimsPrincipal? GetCurrentPrincipal();
    void SetAuthnRequestInSession(AuthnRequest authnRequest);
    AuthnRequest? GetAuthnRequestFromSessionAndRemove();
    void SetLogoutRequestIdInSession(string logoutRequestId);
    string? GetLogoutRequestIdFromSessionAndRemove();
    void SetLoginRelayStateInSession(PendingRelayState relayState);
    PendingRelayState? GetLoginRelayStateFromSessionAndRemove();
    void SetLogoutRelayStateInSession(PendingRelayState relayState);
    PendingRelayState? GetLogoutRelayStateFromSessionAndRemove();
    void SetReturnUrlInSession(string? returnUrl);
    string? GetReturnUrlFromSessionAndRemove();
    Task SignIn(ClaimsPrincipal claimsPrincipal);
    Task SignOut();
}
