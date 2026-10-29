using DK.Gov.Oio.Saml.Configuration;

namespace DK.Gov.Oio.Saml.Interfaces
{
    public interface IIdentityProviderMetadataService
    {
        // Throws InvalidOperationException when the metadata is malformed or is not for the configured IdP.
        IdentityProviderMetadata GetMetadata();
    }
}
