namespace DK.Gov.Oio.Saml.Services;

public class SignedLogoutRequest(string request, bool isRedirectRequest)
{
    public string Request { get; } = request;
    public bool IsRedirectRequest { get; } = isRedirectRequest;
}


public class SignedLogoutResponse(string response, bool isRedirectResponse)
{
    public string Response { get; } = response;
    public bool IsRedirectResponse { get; } = isRedirectResponse;
}