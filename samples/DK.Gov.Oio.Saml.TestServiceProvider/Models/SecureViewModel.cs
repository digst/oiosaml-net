using System.Security.Claims;

namespace DK.Gov.Oio.Saml.TestServiceProvider.Models;

public class SecureViewModel(ClaimsIdentity? userIdentity, string? relayState) : TopViewModel(userIdentity)
{
    public List<Claim> Claims { get; } = userIdentity?.Claims.ToList() ?? [];    
    public string RelayState { get; } = string.IsNullOrEmpty(relayState) ? "None" : relayState;
    
}