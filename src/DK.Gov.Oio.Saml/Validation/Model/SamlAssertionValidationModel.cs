using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Validation.Model;

internal class SamlAssertionValidationModel
{
    public Assertion Assertion { get; set; } = null!;
    public AuthnRequest AuthnRequest { get; set; } = null!;
}
