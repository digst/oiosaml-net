using System.Xml;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Validation.Model;

internal class SamlAuthnResponseValidationModel
{
    public XmlElement Response { get; set; } = null!;
    public XmlElement? AssertionXml { get; set; }
    public Assertion? Assertion { get; set; }
    public AuthnRequest? AuthnRequest { get; set; }
}
