using System.Collections.Specialized;
using DK.Gov.Oio.Saml.Interfaces;

namespace DK.Gov.Oio.Saml.AspNet.Model;

public class SamlLoginResponse : ISamlLoginResponse
{
    public string? LoginResponse { get; private set; }
    public string? RelayState { get; private set; }

    public static SamlLoginResponse From(NameValueCollection form) => new()
    {
        LoginResponse = form.Value("SAMLResponse"),
        RelayState = form.Value("RelayState"),
    };
}
