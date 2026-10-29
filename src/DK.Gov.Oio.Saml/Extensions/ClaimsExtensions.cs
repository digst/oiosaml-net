using System.Security.Claims;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Extensions;

internal static class ClaimsExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public string? GetClaimSubject() => principal.GetClaimValue(ClaimTypes.Name);

        public string? GetIssuer() => principal.GetClaimValue(ClaimsConstants.Issuer);

        public string? GetClaimValue(string claimType) => principal.FindFirst(claimType)?.Value;
    }
}
