using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Builders;

internal class Saml20AuthnRequestBuilder(
    OioSamlOptions options,
    IIdentityProviderMetadataService identityProviderMetadataService,
    IEnumerable<IAuthnRequestProcessor> authnRequestProcessors,
    TimeProvider timeProvider)
{
    public Saml20AuthnRequest CreateSamlAuthnRequest(IAuthnRequestParameters parameters)
    {
        var metadata = identityProviderMetadataService.GetMetadata();
        var request = new Saml20AuthnRequest(options.IdentityProvider, metadata, timeProvider.GetUtcNow().UtcDateTime)
        {
            Issuer = options.ServiceProvider.EntityId,
            Destination = metadata.SingleSignOnService.Location,
        };
        var audience = new AudienceRestriction { Audience = [options.ServiceProvider.EntityId] };
        if (options.IdentityProvider.OneTimeUse) request.SetConditions(audience, new OneTimeUse());
        else request.SetConditions(audience);
        request.IsPassive = FromRequest(parameters)?.IsPassive ?? request.Endpoint.IsPassive;
        request.ForceAuthn = FromRequest(parameters)?.ForceAuthn ?? request.Endpoint.ForceAuthn;
        SetExtensions(request, parameters);
        SetRequestedAuthnContext(request, parameters);

        foreach (var processor in authnRequestProcessors) processor.Process(request);
        return request;
    }

    // What a login requires comes from the configuration; the request's own parameters only for a test SP that allows them.
    private IAuthnRequestParameters? FromRequest(IAuthnRequestParameters parameters) =>
        options.Login.AllowParametersFromRequest ? parameters : null;

    // [OIO-SP-06] "minimum" applies to a requested LoA; a profile alone is requested with the default "exact" comparison.
    private void SetRequestedAuthnContext(Saml20AuthnRequest request, IAuthnRequestParameters parameters)
    {
        var loa = RequestedLoa(parameters);
        var classRefs = new[] { loa, RequestedProfile(parameters) }.OfType<string>().ToArray();
        if (classRefs.Length == 0) return;
        request.Request.RequestedAuthnContext = new RequestedAuthnContext
        {
            Comparison = AuthnContextComparisonType.minimum,
            ComparisonSpecified = loa != null,
            ItemsElementName = classRefs.Select(_ => ItemsChoiceType7.AuthnContextClassRef).ToArray(),
            Items = classRefs,
        };
    }

    private string? RequestedLoa(IAuthnRequestParameters parameters)
    {
        var levelOfAssurance = options.LevelOfAssurance;
        var minimum = levelOfAssurance.RequestGenericLoa ? levelOfAssurance.MinimumLoa : levelOfAssurance.GetMinimumNsisLoa();
        var requested = FromRequest(parameters)?.Loa;
        if (requested != null && !LoAValues.AllValuesRanked.Contains(requested))
            throw new Saml20Exception($"Received unknown LoA: \"{requested}\". Valid values are {string.Join(", ", LoAValues.AllValuesRanked)}");
        if (requested != null && LoAValues.GetLoARank(requested) < LoAValues.GetLoARank(minimum))
            throw new Saml20Exception($"[OIO-SP-16] Requested LoA {requested} is below the configured minimum {minimum}.");
        var loa = requested ?? (levelOfAssurance.RequestMinimumLoa ? minimum : null);
        if (loa == null) return null;
        return (levelOfAssurance.RequestGenericLoa ? OioSaml4Constants.GenericLoAPrefix : OioSaml4Constants.NsisLoAPrefix) + "/" + loa;
    }

    private string? RequestedProfile(IAuthnRequestParameters parameters)
    {
        var profile = FromRequest(parameters)?.Profile ?? options.Login.Profile;
        if (profile == null) return null;
        if (!OioSaml3Profiles.All.Contains(profile))
            throw new Saml20Exception($"Received unknown profile: \"{profile}\". Valid values are {string.Join(", ", OioSaml3Profiles.All)}");
        return OioSaml3Profiles.AuthnContextClassRefPrefix + profile;
    }

    private void SetExtensions(Saml20AuthnRequest request, IAuthnRequestParameters parameters)
    {
        var owner = request.GetXml();
        var extensions = new List<XmlElement>();
        if (parameters.AppSwitchPlatform != null)
            extensions.Add(Serialization.Serialize(AppSwitchFor(parameters.AppSwitchPlatform), owner));
        if (RequestedAttributeProfiles(parameters) is { Length: > 0 } profiles)
            extensions.Add(Serialization.Serialize(new RequestedAttributeProfiles { Profiles = profiles }, owner));
        if (extensions.Count > 0)
            request.Request.Extensions = new Schema.Protocol.Extensions { Any = extensions.ToArray() };
    }

    private string[] RequestedAttributeProfiles(IAuthnRequestParameters parameters)
    {
        var fromRequest = FromRequest(parameters)?.AttributeProfiles;
        var profiles = fromRequest is { Length: > 0 } ? fromRequest : options.Login.AttributeProfiles.ToArray();
        if (profiles.FirstOrDefault(p => !OioSaml4AttributeProfiles.AllProfiles.Contains(p)) is { } invalid)
            throw new Saml20Exception($"Unknown attribute profile requested: {invalid}");
        return profiles;
    }

    private AppSwitch AppSwitchFor(string platform)
    {
        if (options.AppSwitchReturnUrls.SingleOrDefault(x => x.Platform.ToString() == platform)?.Value is { } returnUrl)
            return new AppSwitch { Platform = (AppSwitchPlatform)Enum.Parse(typeof(AppSwitchPlatform), platform), ReturnURL = returnUrl };
        if (options.AppSwitchReturnUrls.Count == 0)
            throw new Saml20Exception("AppSwitch platform configuration is missing.");
        throw new Saml20Exception($"AppSwitch platform not recognized. Options are: {string.Join(", ", options.AppSwitchReturnUrls.Select(x => x.Platform))}");
    }
}
