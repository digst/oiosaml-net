namespace DK.Gov.Oio.Saml.Interfaces
{
    public interface IAuthnRequestProcessor
    {
         void Process(Saml20AuthnRequest authnRequestToUpdate);
    }
    
    // Registered by default; a host adds its own processors to adjust the AuthnRequest before it is sent.
    public class NoopAuthnRequestProcessor : IAuthnRequestProcessor
    {
        public void Process(Saml20AuthnRequest authnRequestToUpdate)
        {
        }
    }
}