using System.Collections.Generic;

namespace DK.Gov.Oio.Saml.Configuration
{
    public class IdentityProviderMetadata(string entityId, IdentityProviderEndpointService singleSignOnService)
    {
        public string EntityId { get; } = entityId;
        public IdentityProviderEndpointService SingleSignOnService { get; } = singleSignOnService;
        public List<IdentityProviderEndpointService> SingleLogoutServices { get; set; } = [];
        public List<string> SigningCertificates { get; set; } = [];
    }
}