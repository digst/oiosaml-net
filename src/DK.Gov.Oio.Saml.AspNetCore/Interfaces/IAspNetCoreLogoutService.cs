using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DK.Gov.Oio.Saml.AspNetCore.Interfaces;

public interface IAspNetCoreLogoutService
{
    Task<IActionResult> ProcessLogout(ILogoutParameters logoutParameters);
}