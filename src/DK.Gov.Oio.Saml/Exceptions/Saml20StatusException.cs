using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Exceptions;

// The IdP answered with a status other than Success, e.g. NoPassive.
public class Saml20StatusException(string msg, Status status) : Saml20ValidationException(msg)
{
    public string? StatusCode { get; } = status.StatusCode?.Value;
    public string? SubStatusCode { get; } = status.StatusCode?.SubStatusCode?.Value;
    public string? StatusMessage { get; } = status.StatusMessage;
}
