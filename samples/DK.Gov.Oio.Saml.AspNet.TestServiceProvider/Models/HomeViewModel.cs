using System.Web.Mvc;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.AspNet.TestServiceProvider.Models;

public class HomeViewModel(OioSamlOptions options, bool isOiosaml4)
{
    public bool IsOiosaml4 { get; } = isOiosaml4;

    public string IdentityProviderEntityId { get; } = options.IdentityProvider.EntityId;

    public List<SelectListItem> AvailableAssuranceLevels { get; } =
    [
        new() { Text = "No assurance level selected", Value = "", Selected = true },
        new() { Text = LoAValues.Low, Value = LoAValues.Low },
        new() { Text = LoAValues.Substantial, Value = LoAValues.Substantial },
        new() { Text = LoAValues.High, Value = LoAValues.High },
    ];

    // The sample's OIOSAML 4 registration (sp-oiosaml4-metadata.xml) does not support DK Person without CPR.
    public List<string> AvailableAttributeProfiles { get; } =
        OioSaml4AttributeProfiles.AllProfiles.Where(p => p != OioSaml4AttributeProfiles.DkPersonWithoutCpr).ToList();
}
