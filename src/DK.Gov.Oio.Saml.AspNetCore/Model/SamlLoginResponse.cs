using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DK.Gov.Oio.Saml.AspNetCore.Model;

public class SamlLoginResponse : ISamlLoginResponse
{
    [FromForm(Name = "SAMLResponse")]
    public string? LoginResponse { get; set; }

    [FromForm(Name = "RelayState")] 
    public string? RelayState { get; set; }
}