using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Configuration
{
    public class IdentityProviderEndpointService(string binding, string location)
    {
        public string Binding { get; } = binding;
        public string Location { get; } = location;
        
        public bool IsRedirectBinding() => Binding == Saml20Constants.ProtocolBindings.HTTP_Redirect;
        public bool IsPostBinding() => Binding == Saml20Constants.ProtocolBindings.HTTP_Post;
    }
}