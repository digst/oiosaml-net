using System.Globalization;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DK.Gov.Oio.Saml.AspNetCore.Security;

// Enforces NemLog-in's session limits on the login cookie: an idle timeout and a maximum length counted from sign-in.
internal static class SessionLifetime
{
    // The cookie's IssuedUtc moves on every sliding renewal, so the sign-in time is kept in the ticket's own properties.
    public const string SignedInKey = "oiosaml.signedIn";

    public static AuthenticationProperties SignedInAt(DateTimeOffset now) =>
        new(new Dictionary<string, string?> { [SignedInKey] = now.ToString("o", CultureInfo.InvariantCulture) });

    public static void Apply(CookieAuthenticationOptions cookie, SessionOptions session)
    {
        cookie.ExpireTimeSpan = session.IdleTimeout;
        cookie.SlidingExpiration = true;
        var next = cookie.Events.OnValidatePrincipal;
        cookie.Events.OnValidatePrincipal = async context =>
        {
            var now = (context.Options.TimeProvider ?? TimeProvider.System).GetUtcNow();
            if (!context.Properties.Items.TryGetValue(SignedInKey, out var value) || value == null
                || session.HasEnded(DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture), now))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(context.Scheme.Name);
                return;
            }
            await next(context);
        };
    }
}
