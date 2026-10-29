namespace DK.Gov.Oio.Saml.TestServiceProvider;

public class AppSettings
{
    public string OiosamlProfile { get; set; } = "oiosaml4";

    public bool IsOiosaml4 => OiosamlProfile == "oiosaml4";
}
