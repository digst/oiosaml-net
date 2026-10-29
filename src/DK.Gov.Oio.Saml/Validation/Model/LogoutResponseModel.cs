using System.Xml;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Validation.Model;

internal class LogoutResponseModel
{
    public XmlElement RawXml { get; set; } = null!;
    public LogoutResponse Response { get; set; } = null!;
    public ILogoutParameters LogoutParameters { get; set; } = null!;
    public string? ExpectedInResponseTo { get; set; }
}
