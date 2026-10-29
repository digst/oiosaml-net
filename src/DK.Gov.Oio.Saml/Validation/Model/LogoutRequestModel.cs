using System.Xml;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Validation.Model;

internal class LogoutRequestModel
{
    public XmlElement RawXml { get; set; } = null!;
    public LogoutRequest Request { get; set; } = null!;
    public ILogoutParameters LogoutParameters { get; set; } = null!;
    public NameID? LoggedInSubject { get; set; }
}
