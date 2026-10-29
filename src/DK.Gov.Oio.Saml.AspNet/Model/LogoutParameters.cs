using System.Collections.Specialized;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Interfaces;

namespace DK.Gov.Oio.Saml.AspNet.Model;

public class LogoutParameters : ILogoutParameters
{
    public string Binding { get; private set; } = "";
    public string? LogoutRequest { get; private set; }
    public string? LogoutResponse { get; private set; }
    public string? RelayState { get; private set; }
    public string? SigAlg { get; private set; }
    public string? Signature { get; private set; }
    public string? RawQuery { get; private set; }

    public static LogoutParameters FromQuery(NameValueCollection query, string? rawQuery) =>
        From(query, Saml20Constants.ProtocolBindings.HTTP_Redirect, rawQuery);

    public static LogoutParameters FromForm(NameValueCollection form) =>
        From(form, Saml20Constants.ProtocolBindings.HTTP_Post, null);

    private static LogoutParameters From(NameValueCollection values, string binding, string? rawQuery) => new()
    {
        Binding = binding,
        LogoutRequest = values.Value("SAMLRequest"),
        LogoutResponse = values.Value("SAMLResponse"),
        RelayState = values.Value("RelayState"),
        SigAlg = values.Value("SigAlg"),
        Signature = values.Value("Signature"),
        RawQuery = rawQuery,
    };
}
