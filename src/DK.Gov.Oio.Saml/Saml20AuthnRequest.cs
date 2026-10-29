using System;
using System.Linq;
using System.Xml;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml;

public class Saml20AuthnRequest(IdentityProvider endpoint, IdentityProviderMetadata endpointMetadata, DateTime issueInstant)
{
    // The IdP the request is for; IAuthnRequestProcessor implementations may read it.
    public IdentityProvider Endpoint { get; } = endpoint;
    public IdentityProviderMetadata EndpointMetadata { get; } = endpointMetadata;

    public AuthnRequest Request { get; } = new()
    {
        Version = Saml20Constants.Version,
        ID = "id" + Guid.NewGuid().ToString("N"),
        Issuer = new NameID(),
        IssueInstant = issueInstant,
    };

    public string Destination
    {
        get => Request.Destination;
        set => Request.Destination = value;
    }

    public bool? ForceAuthn
    {
        get => Request.ForceAuthn;
        set => Request.ForceAuthn = value;
    }

    public bool? IsPassive
    {
        get => Request.IsPassive;
        set => Request.IsPassive = value;
    }

    public string Issuer
    {
        get => Request.Issuer.Value;
        set => Request.Issuer.Value = value;
    }

    public void SetConditions(params ConditionAbstract[] conditions) => Request.Conditions = new Conditions { Items = conditions.ToList() };

    public XmlDocument GetXml() => Serialization.ToXmlDocument(Request);
}
