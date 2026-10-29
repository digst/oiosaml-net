namespace DK.Gov.Oio.Saml.Interfaces;

public interface ILogoutParameters
{
    string Binding { get; }
    string? LogoutRequest { get; }
    string? LogoutResponse { get; }
    string? RelayState { get; }
    string? SigAlg { get; }
    string? Signature { get; }
    // The query string exactly as received (HTTP-Redirect only); the redirect-binding signature covers its encoded form.
    string? RawQuery { get; }
}