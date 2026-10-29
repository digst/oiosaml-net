using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DK.Gov.Oio.Saml.AspNetCore.Model;

public class LogoutQueryParameters : ILogoutParameters
{
    public string Binding => Saml20Constants.ProtocolBindings.HTTP_Redirect;

    [FromQuery(Name = "SAMLRequest")]
    public string? LogoutRequest { get; set;}
    
    [FromQuery(Name = "SAMLResponse")]
    public string? LogoutResponse { get; set;}

    [FromQuery(Name = "RelayState")] 
    public string? RelayState { get; set;}

    [FromQuery(Name = "SigAlg")]
    public string? SigAlg { get; set;}
    
    [FromQuery(Name = "Signature")]
    public string? Signature { get; set;}

    [BindNever]
    public string? RawQuery { get; set; }
}

public class LogoutFormParameters : ILogoutParameters
{ 
    public string Binding => Saml20Constants.ProtocolBindings.HTTP_Post;
    
    [FromForm(Name = "SAMLRequest")]
    public string? LogoutRequest { get; set;}
    
    [FromForm(Name = "SAMLResponse")]
    public string? LogoutResponse { get; set;}

    [FromForm(Name = "RelayState")] 
    public string? RelayState { get; set;}

    [FromForm(Name = "SigAlg")] 
    public string? SigAlg { get; set; }

    [FromForm(Name = "Signature")]
    public string? Signature { get; set;}

    public string? RawQuery => null;
}