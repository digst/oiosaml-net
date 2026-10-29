using System;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml;

internal class Saml20LogoutResponse(DateTime issueInstant)
{
    public LogoutResponse Response { get; } = new()
    {
        Version = Saml20Constants.Version,
        ID = "id" + Guid.NewGuid().ToString("N"),
        Issuer = new NameID(),
        IssueInstant = issueInstant,
        Status = new Status { StatusCode = new StatusCode() },
    };

    public string ID => Response.ID;

    public string StatusCode
    {
        get => Response.Status.StatusCode.Value;
        set => Response.Status.StatusCode.Value = value;
    }

    public string Issuer
    {
        get => Response.Issuer.Value;
        set => Response.Issuer.Value = value;
    }

    public string Destination
    {
        get => Response.Destination;
        set => Response.Destination = value;
    }

    public string InResponseTo
    {
        get => Response.InResponseTo;
        set => Response.InResponseTo = value;
    }

    public XmlDocument GetXml() => Serialization.ToXmlDocument(Response);
}
