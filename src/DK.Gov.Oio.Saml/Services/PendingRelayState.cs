using System;
using System.Security.Cryptography;
using System.Text;
using DK.Gov.Oio.Saml.Exceptions;

namespace DK.Gov.Oio.Saml.Services;

public sealed class PendingRelayState(string token, string? value)
{
    public const int MaxLength = 80;

    public string Token { get; } = token;
    public string? Value { get; } = value;

    public static PendingRelayState Issue(string? value) => new(NewToken(), string.IsNullOrEmpty(value) ? null : value);

    public static string? Redeem(PendingRelayState? pending, string? received)
    {
        if (pending?.Token != (string.IsNullOrEmpty(received) ? null : received))
            throw new Saml20ValidationException("RelayState does not match the one sent with the request.");
        return pending?.Value;
    }

    public static bool IsWithinLimit(string? relayState) => relayState == null || Encoding.UTF8.GetByteCount(relayState) <= MaxLength;

    private static string NewToken()
    {
        var bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
