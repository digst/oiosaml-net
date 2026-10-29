namespace DK.Gov.Oio.Saml.Interfaces
{
    public interface IAuthnRequestParameters
    {
        bool? ForceAuthn { get; }
        bool? IsPassive { get; }
        string? Loa { get; }
        string? AppSwitchPlatform { get; }
        // OIOSAML 3: OioSaml3Profiles.Person or Professional.
        string? Profile { get; }
        string[] AttributeProfiles { get; }
        string? RelayState { get;}
        string? ReturnUrl { get; }
    }
}