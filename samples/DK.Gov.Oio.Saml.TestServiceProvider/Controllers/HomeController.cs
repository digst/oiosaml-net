using System.Diagnostics;
using System.Security.Claims;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DK.Gov.Oio.Saml.TestServiceProvider.Models;

namespace DK.Gov.Oio.Saml.TestServiceProvider.Controllers;

public class HomeController(IHttpContextAccessor httpContextAccessor, OioSamlOptions options, IOptions<AppSettings> appSettings) : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View(GetViewModel());
    }

    private HomeViewModel GetViewModel()
    {
        return new HomeViewModel((ClaimsIdentity?)httpContextAccessor.HttpContext?.User?.Identity, options, appSettings.Value.IsOiosaml4);
    }

    [Authorize]
    public IActionResult Secure()
    {
        return View(CreateSecureViewModel());
    }

    private SecureViewModel CreateSecureViewModel()
    {
        var relayState = httpContextAccessor.HttpContext?.Request.Query["RelayState"];
        return new SecureViewModel((ClaimsIdentity?)httpContextAccessor.HttpContext?.User?.Identity, relayState);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel((ClaimsIdentity?)HttpContext.User.Identity, Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}