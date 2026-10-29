using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DK.Gov.Oio.Saml.AspNetCore.Model;

public class AuthnRequestParameters : IAuthnRequestParameters
{
    [FromQuery(Name = "forceAuthn")]
    public bool? ForceAuthn { get; set; }
    
    [FromQuery(Name = "isPassive")]
    public bool? IsPassive { get; set; }
    
    [FromQuery(Name = "levelOfAssurance")]
    public string? Loa { get; set; }
    
    [FromQuery(Name = "appSwitchPlatform")]
    public string? AppSwitchPlatform { get; set; }
    
    [FromQuery(Name = "profile")]
    public string? Profile { get; set; }

    [FromQuery(Name = "attributeProfiles")]
    public string[] AttributeProfiles { get; set; } = [];

    [FromQuery(Name = "RelayState")] 
    public string? RelayState { get; set; }

    [FromQuery(Name = "ReturnUrl")]
    public string? ReturnUrl { get; set; }
}