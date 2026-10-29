using System;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml;

internal class Saml20LogoutRequest(DateTime issueInstant)
{
    public LogoutRequest Request { get; } = new()
    {
        Version = Saml20Constants.Version,
        ID = "id" + Guid.NewGuid().ToString("N"),
        Issuer = new NameID(),
        IssueInstant = issueInstant,
    };

    public string ID => Request.ID;

    // A URI, e.g. Saml20Constants.Reasons.User.
    public string Reason
    {
        get => Request.Reason;
        set => Request.Reason = value;
    }

    public NameID? SubjectToLogOut
    {
        get => Request.Item as NameID;
        set => Request.Item = value;
    }

    public string Destination
    {
        get => Request.Destination;
        set => Request.Destination = value;
    }

    public string SessionIndex
    {
        get => Request.SessionIndex[0];
        set => Request.SessionIndex = [value];
    }

    public string Issuer
    {
        get => Request.Issuer.Value;
        set => Request.Issuer.Value = value;
    }

    public XmlDocument GetXml() => Serialization.ToXmlDocument(Request);
}
