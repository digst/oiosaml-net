using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DK.Gov.Oio.Saml.AspNetCore.Interfaces;

public interface IAspNetCoreLoginService
{
    IActionResult InitiateLogin(IAuthnRequestParameters authnRequestParameters);
    Task<IActionResult> ProcessLoginResponse(ISamlLoginResponse loginResponse);
}