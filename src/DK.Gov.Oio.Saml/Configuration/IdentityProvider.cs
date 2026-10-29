namespace DK.Gov.Oio.Saml.Configuration;

// The one IdP the service provider federates with; each environment (DevTest4, integration test, production) has its own configuration.
public class IdentityProvider
{
    public string EntityId { get; set; } = "";
    public string MetadataFile { get; set; } = "";
    public bool ForceAuthn { get; set; }
    public bool IsPassive { get; set; }
    public bool OneTimeUse { get; set; }

    public string GetMetadataFile() => ConfiguredPath.Resolve(MetadataFile);
}
