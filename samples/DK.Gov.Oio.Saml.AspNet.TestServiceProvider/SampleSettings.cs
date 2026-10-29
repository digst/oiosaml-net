namespace DK.Gov.Oio.Saml.AspNet.TestServiceProvider;

public class SampleSettings(string oiosamlProfile)
{
    public string OiosamlProfile { get; } = oiosamlProfile;

    public bool IsOiosaml4 => OiosamlProfile == "oiosaml4";
}
