using System.Globalization;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace DK.Gov.Oio.Saml.AspNet.Security;

// Enforces NemLog-in's session limits on the login cookie: an idle timeout and a maximum length counted from sign-in.
internal static class SessionLifetime
{
    // The cookie's IssuedUtc moves on every sliding renewal, so the sign-in time is kept in the ticket's own properties.
    public const string SignedInKey = "oiosaml.signedIn";

    public static AuthenticationProperties SignedInAt(DateTimeOffset now) =>
        new(new Dictionary<string, string> { [SignedInKey] = now.ToString("o", CultureInfo.InvariantCulture) });

    public static void Apply(CookieAuthenticationOptions cookie, SessionOptions session)
    {
        cookie.ExpireTimeSpan = session.IdleTimeout;
        cookie.SlidingExpiration = true;
        cookie.Provider ??= new CookieAuthenticationProvider();
        if (cookie.Provider is not CookieAuthenticationProvider provider)
            throw new InvalidOperationException($"The cookie Provider must be a {nameof(CookieAuthenticationProvider)}; set its events instead of replacing it.");
        var next = provider.OnValidateIdentity;
        provider.OnValidateIdentity = context =>
        {
            if (!context.Properties.Dictionary.TryGetValue(SignedInKey, out var value) || value == null
                || session.HasEnded(DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture), context.Options.SystemClock.UtcNow))
            {
                context.RejectIdentity();
                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
                return Task.CompletedTask;
            }
            return next(context);
        };
    }
}
