namespace DK.Gov.Oio.Saml.Interfaces;

public interface ISamlLoginResponse
{
    string? LoginResponse { get; }
    string? RelayState { get; }
}