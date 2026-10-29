using System.Collections.Specialized;

namespace DK.Gov.Oio.Saml.AspNet.Model;

// An empty parameter counts as absent, as ASP.NET Core's model binding has it; a form field left blank arrives as "name=".
internal static class ParameterValues
{
    public static string? Value(this NameValueCollection values, string name) => string.IsNullOrEmpty(values[name]) ? null : values[name];
}
