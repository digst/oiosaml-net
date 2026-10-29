using System.Security.Claims;

namespace DK.Gov.Oio.Saml.TestServiceProvider.Models;

public abstract class TopViewModel(ClaimsIdentity? userIdentity)
{
    public string SubjectName => !IsAuthenticated ? "Not LoggedIn" : userIdentity?.Name?? "Unknown";
    
    public bool IsAuthenticated => userIdentity?.IsAuthenticated == true;
}