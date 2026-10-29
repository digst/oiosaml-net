using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.Interfaces;

public interface ISaml20LoginService
{
    string GetSignedAuthnRequestString(IAuthnRequestParameters authnRequestParameters);
    Task<LoginResult> ProcessAuthenticationResponse(ISamlLoginResponse loginResponse);
}