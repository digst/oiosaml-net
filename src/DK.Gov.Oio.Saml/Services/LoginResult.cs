namespace DK.Gov.Oio.Saml.Services;

public sealed class LoginResult(string? returnUrl, string? relayState)
{
    public string? ReturnUrl { get; } = returnUrl;
    public string? RelayState { get; } = relayState;
}
