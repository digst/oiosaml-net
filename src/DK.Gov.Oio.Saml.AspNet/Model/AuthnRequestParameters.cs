using System.Collections.Specialized;
using DK.Gov.Oio.Saml.Interfaces;

namespace DK.Gov.Oio.Saml.AspNet.Model;

public class AuthnRequestParameters : IAuthnRequestParameters
{
    public bool? ForceAuthn { get; private set; }
    public bool? IsPassive { get; private set; }
    public string? Loa { get; private set; }
    public string? AppSwitchPlatform { get; private set; }
    public string? Profile { get; private set; }
    public string[] AttributeProfiles { get; private set; } = [];
    public string? RelayState { get; private set; }
    public string? ReturnUrl { get; private set; }

    public static AuthnRequestParameters From(NameValueCollection query) => new()
    {
        ForceAuthn = Boolean(query.Value("forceAuthn")),
        IsPassive = Boolean(query.Value("isPassive")),
        Loa = query.Value("levelOfAssurance"),
        AppSwitchPlatform = query.Value("appSwitchPlatform"),
        Profile = query.Value("profile"),
        AttributeProfiles = query.GetValues("attributeProfiles")?.Where(p => !string.IsNullOrEmpty(p)).ToArray() ?? [],
        RelayState = query.Value("RelayState"),
        ReturnUrl = query.Value("ReturnUrl"),
    };

    private static bool? Boolean(string? value) => bool.TryParse(value, out var result) ? result : null;
}
