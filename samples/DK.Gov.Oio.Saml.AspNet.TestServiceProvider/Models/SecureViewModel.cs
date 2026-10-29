using System.Security.Claims;

namespace DK.Gov.Oio.Saml.AspNet.TestServiceProvider.Models;

public class SecureViewModel(ClaimsIdentity? userIdentity, string? relayState)
{
    public List<Claim> Claims { get; } = userIdentity?.Claims.ToList() ?? [];

    public string RelayState { get; } = string.IsNullOrEmpty(relayState) ? "None" : relayState!;
}
