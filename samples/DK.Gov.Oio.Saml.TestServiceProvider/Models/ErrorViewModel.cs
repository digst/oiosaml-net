using System.Security.Claims;

namespace DK.Gov.Oio.Saml.TestServiceProvider.Models;

public class ErrorViewModel(ClaimsIdentity? userIdentity, string? requestId) : TopViewModel(userIdentity)
{
    public string? RequestId { get; } = requestId;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
