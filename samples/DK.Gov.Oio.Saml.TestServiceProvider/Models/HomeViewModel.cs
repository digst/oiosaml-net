using System.Security.Claims;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DK.Gov.Oio.Saml.TestServiceProvider.Models;

public class HomeViewModel(ClaimsIdentity? userIdentity, OioSamlOptions options, bool isOiosaml4): TopViewModel(userIdentity)
{
    
    public bool IsOiosaml4 { get; } = isOiosaml4;

    public string IdentityProviderEntityId { get; } = options.IdentityProvider.EntityId;

    public List<SelectListItem> AvailableAssuranceLevels { get; } = GetAvailableAssuranceLevels();
    
    public List<string> AvailableAttributeProfiles { get; } = GetAvailableAttributeProfiles();
    
    private static List<SelectListItem> GetAvailableAssuranceLevels()
    {
        return new List<SelectListItem>
        {
            new("No assurance level selected", "", selected: true),
            new(LoAValues.Low, LoAValues.Low),
            new(LoAValues.Substantial, LoAValues.Substantial),
            new(LoAValues.High, LoAValues.High)
        };
    }
    
    private static List<string> GetAvailableAttributeProfiles()
    {
        // The sample's OIOSAML 4 registration (Metadata/sp-oiosaml4-metadata.xml) does not support DK Person without CPR.
        return OioSaml4AttributeProfiles.AllProfiles.Where(p => p != OioSaml4AttributeProfiles.DkPersonWithoutCpr).ToList();
    }
    
}