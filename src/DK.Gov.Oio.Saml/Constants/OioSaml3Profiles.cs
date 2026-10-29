namespace DK.Gov.Oio.Saml.Constants;

// OIOSAML 3 asks for a person or professional identity with an AuthnContextClassRef; OIOSAML 4 uses attribute profiles instead.
public static class OioSaml3Profiles
{
    public const string Person = "Person";
    public const string Professional = "Professional";
    public const string AuthnContextClassRefPrefix = "https://data.gov.dk/eid/";
    public const string NameIdPrefix = "https://data.gov.dk/model/core/eid/";

    public static readonly string[] All = [Person, Professional];

    public static string GetNameIdPrefix(string profile) => NameIdPrefix + profile.ToLowerInvariant() + "/";
}
