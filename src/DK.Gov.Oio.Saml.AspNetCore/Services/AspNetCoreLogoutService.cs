using System.Net;
using DK.Gov.Oio.Saml.AspNetCore.Interfaces;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace DK.Gov.Oio.Saml.AspNetCore.Services;

public class AspNetCoreLogoutService(ISaml20LogoutService saml20LogoutService, OioSamlOptions options) : IAspNetCoreLogoutService
{
    public async Task<IActionResult> ProcessLogout(ILogoutParameters logoutParameters)
    {
        if (logoutParameters.IsSpInitiatedLogout())
        {
            return await HandleSpInitiatedLogout(logoutParameters);
        }

        if (logoutParameters.IsIdPInitiatedLogout())
        {
            return await HandleIdPInitiatedLogout(logoutParameters);
        }

        if(logoutParameters.IsLogoutResponse())
        {
            var relayState = await saml20LogoutService.ProcessIdpLogoutResponse(logoutParameters);
            return new RedirectResult(GetRedirectUrl(relayState));
        }

        throw new Saml20Exception("Unhandled Logout scenario");
    }

    private async Task<IActionResult> HandleIdPInitiatedLogout(ILogoutParameters logoutParameters)
    {
        var response = await saml20LogoutService.ProcessIdpInitiatedLogout(logoutParameters);
        
        if (response.IsRedirectResponse)
            return new RedirectResult(response.Response);

        return new ContentResult
        {
            Content = response.Response,
            ContentType = "text/html",
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    private async Task<IActionResult> HandleSpInitiatedLogout(ILogoutParameters logoutParameters)
    {
        var signedLogoutRequest = await saml20LogoutService.ProcessSpInitiatedLogout(logoutParameters);

        if (signedLogoutRequest.IsRedirectRequest)
            return new RedirectResult(signedLogoutRequest.Request);

        return new ContentResult
        {
            Content = signedLogoutRequest.Request,
            ContentType = "text/html",
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    private string GetRedirectUrl(string? relayState)
    {
        var redirectUrl = options.ServiceProvider.LogoutRedirectUrl;
        return string.IsNullOrEmpty(relayState) ? 
            redirectUrl : 
            QueryHelpers.AddQueryString(redirectUrl, "RelayState", relayState);
    }
}