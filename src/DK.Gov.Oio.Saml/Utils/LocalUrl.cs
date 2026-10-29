using System.Linq;

namespace DK.Gov.Oio.Saml.Utils;

internal static class LocalUrl
{
    // Same rules as ASP.NET Core's IsLocalUrl; browsers strip control characters, so "/\t/evil.test" would become "//evil.test".
    public static bool IsLocal(string? url)
    {
        if (url == null || url.Length == 0 || url.Any(char.IsControl)) return false;
        if (url[0] == '/') return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
        return url.Length > 1 && url[0] == '~' && url[1] == '/' && (url.Length == 2 || (url[2] != '/' && url[2] != '\\'));
    }
}
