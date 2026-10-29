using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.Interfaces;

public interface ISaml20LogoutService
{
    Task<SignedLogoutRequest> ProcessSpInitiatedLogout(ILogoutParameters logoutParameters);
    Task<string?> ProcessIdpLogoutResponse(ILogoutParameters logoutParameters);
    Task<SignedLogoutResponse> ProcessIdpInitiatedLogout(ILogoutParameters logoutParameters);
}