using DK.Gov.Oio.Saml.AspNetCore.Interfaces;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace DK.Gov.Oio.Saml.AspNetCore.Services;

public class AspNetCoreLoginService(ISaml20LoginService saml20LoginService, OioSamlOptions options) : IAspNetCoreLoginService
{
    public IActionResult InitiateLogin(IAuthnRequestParameters authnRequestParameters)
    {
        var request = saml20LoginService.GetSignedAuthnRequestString(authnRequestParameters);
        
        return new RedirectResult(request);
    }
    
    public async Task<IActionResult> ProcessLoginResponse(ISamlLoginResponse loginResponse)
    {
        var result = await saml20LoginService.ProcessAuthenticationResponse(loginResponse);

        return new RedirectResult(GetRedirectUrl(result));
    }

    private string GetRedirectUrl(LoginResult result)
    {
        var redirectUrl = result.ReturnUrl ?? options.ServiceProvider.LoginRedirectUrl;
        return string.IsNullOrEmpty(result.RelayState) ?
            redirectUrl :
            QueryHelpers.AddQueryString(redirectUrl, "RelayState", result.RelayState);
    }
}