using System;
using System.Collections.Generic;
using System.Linq;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Validation.Model;
using FluentValidation;

namespace DK.Gov.Oio.Saml.Validation;

internal class SamlAssertionValidator : AbstractValidator<SamlAssertionValidationModel>
{
    public SamlAssertionValidator(OioSamlOptions options, TimeProvider timeProvider)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;
        var skew = options.AllowedClockSkew;
        var acsUrl = options.ServiceProvider.GetAssertionConsumerServiceUrl();

        RuleFor(m => m.Assertion).NotNull().WithMessage("Assertion must be set.");
        RuleFor(m => m.AuthnRequest).NotNull().WithMessage("AuthnRequest must be set.");
        RuleFor(m => m.Assertion.Version).Equal(Saml20Constants.Version).WithMessage("Assertion Version must be 2.0.");
        RuleFor(m => m.Assertion.ID).Must(id => id?.Length >= 16).WithMessage("Assertion ID must be at least 16 characters (128 bits).");
        RuleFor(m => m.Assertion.IssueInstant).NotNull().WithMessage("Assertion must have an IssueInstant.");

        RuleFor(m => m.Assertion.Issuer).NotNull().WithMessage("[OIO-IDP-14] Assertion must contain an Issuer.");
        RuleFor(m => m.Assertion.Issuer.Value).NotEmpty().WithMessage("[OIO-IDP-14] Assertion must contain an Issuer.")
            .Must(v => v.Length <= 256).WithMessage("[OIO-GE-03] Issuer must not exceed 256 characters.");
        RuleFor(m => m.Assertion.Issuer.Format).Must(f => f is null or Saml20Constants.NameIdentifierFormats.Entity)
            .WithMessage("[OIO-IDP-14] Issuer Format must be omitted or urn:oasis:names:tc:SAML:2.0:nameid-format:entity.");

        RuleFor(m => m.Assertion.Subject).NotNull().WithMessage("[OIO-IDP-15] Assertion must contain a Subject with a NameID.");
        RuleFor(m => m.Assertion.Subject.Items).NotNull().WithMessage("[OIO-IDP-15] Assertion must contain a Subject with a NameID.")
            .Must(items => !items.OfType<EncryptedElement>().Any()).WithMessage("[OIO-IDP-13] EncryptedID is not allowed.")
            .Must(items => items.OfType<NameID>().Count() == 1).WithMessage("[OIO-IDP-15] Subject must contain exactly one NameID.");
        RuleFor(m => m.Assertion.Subject.Items.OfType<NameID>().Single())
            .Must(n => n.Format is Saml20Constants.NameIdentifierFormats.Transient or Saml20Constants.NameIdentifierFormats.Persistent)
            .WithMessage("[OIO-IDP-15] NameID Format must be transient or persistent.")
            .Must(n => n.Value?.Length is > 0 and <= 256).WithMessage("NameID value must be 1-256 characters.");

        RuleFor(m => m.Assertion.Subject.Items.OfType<SubjectConfirmation>().FirstOrDefault(c => c.Method == SubjectConfirmation.BEARER_METHOD))
            .NotNull().WithMessage("[OIO-IDP-17] Subject must contain a bearer SubjectConfirmation.")
            .Must(c => c!.Item is not EncryptedElement).WithMessage("[OIO-IDP-13] EncryptedID is not allowed.")
            .Must(c => c!.SubjectConfirmationData != null).WithMessage("[OIO-IDP-17] Bearer SubjectConfirmation must contain SubjectConfirmationData.")
            .Must(c => c!.SubjectConfirmationData.Recipient == acsUrl).WithMessage($"[OIO-IDP-17] SubjectConfirmationData Recipient must be '{acsUrl}'.")
            .Must((m, c) => c!.SubjectConfirmationData.InResponseTo == m.AuthnRequest.ID).WithMessage("SubjectConfirmationData InResponseTo does not match the pending AuthnRequest.")
            .Must(c => c!.SubjectConfirmationData.NotOnOrAfter != null).WithMessage("[OIO-IDP-17] SubjectConfirmationData must contain NotOnOrAfter.")
            .Must(c => timeProvider.GetUtcNow().WithinAllowedPeriod(c!.SubjectConfirmationData.NotBefore, c.SubjectConfirmationData.NotOnOrAfter, skew))
            .WithMessage("[OIO-GE-01] Current time is outside the SubjectConfirmationData validity window.");

        RuleFor(m => m.Assertion.Conditions).NotNull().WithMessage("[OIO-IDP-18] Assertion must contain Conditions.")
            .Must(c => timeProvider.GetUtcNow().WithinAllowedPeriod(c.NotBefore, c.NotOnOrAfter, skew))
            .WithMessage("[OIO-GE-01] Current time is outside the Conditions validity window.");
        RuleFor(m => Audiences(m.Assertion))
            .Must(a => a.Contains(options.ServiceProvider.EntityId)).WithMessage("[OIO-IDP-18] AudienceRestriction must include the service provider's entity id.");

        RuleFor(m => m.Assertion.Items).NotNull()
            .Must(s => s.Length == 2 && s.OfType<AuthnStatement>().Count() == 1 && s.OfType<AttributeStatement>().Count() == 1)
            .WithMessage("[OIO-IDP-11] Assertion must contain exactly one AuthnStatement and exactly one AttributeStatement and no other statements.");
        RuleFor(m => m.Assertion.Items.OfType<AuthnStatement>().Single().AuthnInstant)
            .NotNull().WithMessage("AuthnStatement must contain AuthnInstant.")
            .Must((m, instant) => m.AuthnRequest.ForceAuthn != true || instant >= m.AuthnRequest.IssueInstant?.Subtract(skew))
            .WithMessage("[OIO-SP-15] ForceAuthn was requested but AuthnInstant predates the AuthnRequest.");
        RuleFor(m => m.Assertion.Items.OfType<AttributeStatement>().Single().Items)
            .NotNull().WithMessage("[OIO-IDP-11] AttributeStatement must contain attributes.")
            .Must(a => !a.OfType<EncryptedElement>().Any()).WithMessage("[OIO-IDP-13] EncryptedAttribute is not allowed.")
            .Must(a => a.OfType<SamlAttribute>().All(x => x.NameFormat == Saml20Constants.AttributeNameFormats.Uri))
            .WithMessage("[OIO-AP-03] Attributes must have NameFormat urn:oasis:names:tc:SAML:2.0:attrname-format:uri.");

        RuleFor(m => m).Custom((m, ctx) => Check(ctx, () => ValidateLoA(m, options)));
        RuleFor(m => m).Custom((m, ctx) => Check(ctx, () => ValidateAttributeProfile(m)));
        RuleFor(m => m).Custom((m, ctx) => Check(ctx, () => ValidateRequestedProfile(m)));
    }

    private static List<string> Audiences(Assertion assertion) =>
        assertion.Conditions.Items?.OfType<AudienceRestriction>().SelectMany(a => a.Audience ?? []).ToList() ?? [];

    private static void Check(ValidationContext<SamlAssertionValidationModel> ctx, System.Action validate)
    {
        try { validate(); }
        catch (Saml20ValidationException e) { ctx.AddFailure(e.Message); }
    }

    // [OIO-SP-16] The NSIS or eIDAS LoA is checked whatever was requested; the generic LoA attribute is only delivered when the
    // attribute contract asks for it, so it is never required. DK profiles carry NSIS LoA (eIDAS optional), EU profiles eIDAS LoA.
    private static void ValidateLoA(SamlAssertionValidationModel model, OioSamlOptions options)
    {
        var (attributeName, configuredMinimum) = HasAttribute(model.Assertion, OioSaml4Constants.AttributeName_NsisLoA)
            ? (OioSaml4Constants.AttributeName_NsisLoA, options.LevelOfAssurance.GetMinimumNsisLoa())
            : HasAttribute(model.Assertion, OioSaml4Constants.AttributeName_EidasLoA)
                ? (OioSaml4Constants.AttributeName_EidasLoA, options.LevelOfAssurance.GetMinimumEidasLoa())
                : throw new Saml20ValidationException($"[OIO-SP-16] The assertion carries neither {OioSaml4Constants.AttributeName_NsisLoA} nor {OioSaml4Constants.AttributeName_EidasLoA}.");
        var loaInResponse = GetSingleAttributeValue(model.Assertion, attributeName);
        // The configured minimum is a floor: a request can raise it, never lower it.
        var minimumLoa = configuredMinimum;
        if (GetRequestedLoa(model.AuthnRequest) is { } requestedLoa)
        {
            if (LoAValues.GetLoARank(requestedLoa) == -1) throw new Saml20ValidationException($"Unable to validate LoA. Requested LoA value is not recognized: {requestedLoa}");
            if (LoAValues.GetLoARank(requestedLoa) > LoAValues.GetLoARank(configuredMinimum)) minimumLoa = requestedLoa;
        }

        var minimumLoaRank = LoAValues.GetLoARank(minimumLoa);
        var loaInResponseRank = LoAValues.GetLoARank(loaInResponse);
        if (minimumLoaRank == -1) throw new Saml20ValidationException($"Unable to validate LoA. Minimum LoA value is not recognized: {minimumLoa}");
        if (loaInResponseRank == -1) throw new Saml20ValidationException($"LoA value is not recognized: {loaInResponse}");
        if (loaInResponseRank < minimumLoaRank) throw new Saml20ValidationException($"[OIO-SP-16] LoA is too low. Expected minimum: {minimumLoa}, actual: {loaInResponse}.");
    }

    private static bool HasAttribute(Assertion assertion, string name) => assertion.GetAttributes().Any(x => x.Name == name);

    // The level asked for in the AuthnRequest, whether as the OIOSAML 4 generic or the OIOSAML 3 NSIS class ref.
    private static string? GetRequestedLoa(AuthnRequest authnRequest)
    {
        var prefixes = new[] { OioSaml4Constants.GenericLoAPrefix + "/", OioSaml4Constants.NsisLoAPrefix + "/" };
        var requested = authnRequest.RequestedAuthnContext?.Items?
            .Select(v => prefixes.FirstOrDefault(p => v != null && v.StartsWith(p)) is { } prefix ? v!.Substring(prefix.Length) : null)
            .OfType<string>().ToList() ?? [];
        if (requested.Count > 1) throw new Saml20ValidationException($"Expected a single requested LoA, but found {requested.Count}.");
        return requested.SingleOrDefault();
    }

    private static string GetSingleAttributeValue(Assertion assertion, string attributeName)
    {
        var values = assertion.GetAttributeValues(attributeName).ToList();
        if (values.Count != 1) throw new Saml20ValidationException($"Expected single value for attribute {attributeName} but found {values.Count}.");
        return values[0];
    }

    // OIOSAML 3: a requested person or professional identity shows in the NameID, https://data.gov.dk/model/core/eid/{person|professional}/uuid/...
    private static void ValidateRequestedProfile(SamlAssertionValidationModel model)
    {
        var profile = model.AuthnRequest.RequestedAuthnContext?.Items?
            .Where(v => v != null && v.StartsWith(OioSaml3Profiles.AuthnContextClassRefPrefix))
            .Select(v => v.Substring(OioSaml3Profiles.AuthnContextClassRefPrefix.Length))
            .FirstOrDefault(OioSaml3Profiles.All.Contains);
        if (profile == null) return;
        var nameId = model.Assertion.Subject.Items.OfType<NameID>().Single().Value;
        if (!nameId.StartsWith(OioSaml3Profiles.GetNameIdPrefix(profile), StringComparison.Ordinal))
            throw new Saml20ValidationException($"The AuthnRequest asked for a {profile} identity, but the NameID is {nameId}.");
    }

    // [OIO-SP-07] The IdP returns one of the requested attribute profiles. The profile attribute is mandatory from OIOSAML 4.0.1 on,
    // but an OIOSAML 3 response has none, so it is only required when the request asked for profiles.
    private static void ValidateAttributeProfile(SamlAssertionValidationModel model)
    {
        var requested = model.AuthnRequest.GetAttributeProfiles().ToList();
        if (requested.Count == 0) return;
        var returned = GetSingleAttributeValue(model.Assertion, OioSaml4Constants.AttributeName_AttributeProfile);
        if (!requested.Contains(returned))
            throw new Saml20ValidationException($"[OIO-SP-07] The response carries attribute profile {returned}, which was not requested (requested: {string.Join(", ", requested)}).");
    }

}
