using DK.Gov.Oio.Saml.AspNetCore.Interfaces;
using DK.Gov.Oio.Saml.AspNetCore.Model;
using DK.Gov.Oio.Saml.Extensions;
using MessageLimits = DK.Gov.Oio.Saml.Extensions.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace DK.Gov.Oio.Saml.AspNetCore.Controllers;

[AllowAnonymous] 
// The IdP posts here cross-site without a token; the user's own logout is checked in the action instead.
[IgnoreAntiforgeryToken]
[Route("Auth")]
public class AuthController(
    IAspNetCoreLoginService loginService,
    IAspNetCoreLogoutService logoutService,
    IAntiforgery antiforgery,
    ILogger<AuthController> logger) : Controller
{
    // Behind a proxy RemoteIpAddress is the proxy's unless the host applies UseForwardedHeaders.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        using (logger.BeginScope("ClientIp:{ClientIp}", HttpContext.Connection.RemoteIpAddress?.ToString()))
            await next();
    }

    [Route("Login")]
    public IActionResult Login([FromQuery] AuthnRequestParameters queryParameters)
    {
        return loginService.InitiateLogin(queryParameters);
    }
    
    [Route("AssertionConsumerService")]
    [HttpPost]
    [RequestSizeLimit(MessageLimits.MaximumPostedMessageBytes)]
    public async Task<IActionResult> AssertionConsumerService([FromForm]SamlLoginResponse? loginResponse)
    {
        if (loginResponse == null) return UnreadableForm();
        return await loginService.ProcessLoginResponse(loginResponse);
    }
    
    [Route("logout")]
    [HttpGet]
    public async Task<IActionResult> Logout([FromQuery] LogoutQueryParameters logoutQueryParameters)
    {
        // Without SAML parameters this would be the user's own logout, which only a POST with an antiforgery token may start.
        if (logoutQueryParameters.IsSpInitiatedLogout())
            return StatusCode(StatusCodes.Status405MethodNotAllowed);
        logoutQueryParameters.RawQuery = Request.QueryString.HasValue ? Request.QueryString.Value![1..] : null;
        return await logoutService.ProcessLogout(logoutQueryParameters);
    }
    
    [Route("logout")]
    [HttpPost]
    [RequestSizeLimit(MessageLimits.MaximumPostedMessageBytes)]
    public async Task<IActionResult> Logout([FromForm] LogoutFormParameters? logoutFormParameters)
    {
        if (logoutFormParameters == null) return UnreadableForm();
        if (logoutFormParameters.IsSpInitiatedLogout() && !await antiforgery.IsRequestValidAsync(HttpContext))
        {
            logger.LogWarning("Logout refused: the request has no valid antiforgery token.");
            return BadRequest();
        }
        return await logoutService.ProcessLogout(logoutFormParameters);
    }

    // Model binding leaves the parameter null when the form can't be read, e.g. a body over the size limit.
    private BadRequestResult UnreadableForm()
    {
        logger.LogWarning("SAML message refused: the form could not be read ({Problems}).",
            string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.Exception?.Message ?? e.ErrorMessage)));
        return BadRequest();
    }
}
