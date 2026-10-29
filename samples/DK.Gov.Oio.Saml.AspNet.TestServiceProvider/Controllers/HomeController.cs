using System.Security.Claims;
using System.Web.Mvc;
using DK.Gov.Oio.Saml.AspNet.TestServiceProvider.Models;
using DK.Gov.Oio.Saml.Configuration;

namespace DK.Gov.Oio.Saml.AspNet.TestServiceProvider.Controllers;

public class HomeController(OioSamlOptions options, SampleSettings settings) : Controller
{
    public ActionResult Index() => View(new HomeViewModel(options, settings.IsOiosaml4));

    [Authorize]
    public ActionResult Secure() => View(new SecureViewModel(User.Identity as ClaimsIdentity, Request.Unvalidated.QueryString["RelayState"]));
}
