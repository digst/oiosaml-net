using System;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Validation.Model;
using FluentValidation;

namespace DK.Gov.Oio.Saml.Validation;

internal class SamlLogoutRequestValidator : AbstractValidator<LogoutRequestModel>
{
    public static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(5);

    public SamlLogoutRequestValidator(OioSamlOptions options, IIdentityProviderCertificateStore certificateStore, TimeProvider timeProvider)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;
        var skew = options.AllowedClockSkew;
        var sloUrl = options.ServiceProvider.GetSingleLogoutServiceUrl();

        RuleFor(m => m.Request.Issuer).NotNull().WithMessage("LogoutRequest must contain an Issuer.");
        RuleFor(m => m.Request.Issuer.Value).NotEmpty().WithMessage("LogoutRequest must contain an Issuer.")
            .Must(v => v.Length <= 256).WithMessage("[OIO-GE-03] Issuer must not exceed 256 characters.")
            .Equal(options.IdentityProvider.EntityId).WithMessage(m => $"Issuer '{m.Request.Issuer.Value}' is not the configured identity provider '{options.IdentityProvider.EntityId}'.");
        RuleFor(m => m).CustomAsync(async (m, ctx, cancellationToken) =>
        {
            var trusted = await certificateStore.GetTrustedKeysAsync(cancellationToken);
            if ((trusted.Error ?? SamlMessageSignature.Verify(m.LogoutParameters, m.RawXml, trusted.Keys!)) is { } failure)
                ctx.AddFailure($"[OIO-IDP-23] Invalid LogoutRequest signature: {failure}");
        });
        RuleFor(m => m.Request.Destination).Equal(sloUrl).WithMessage($"LogoutRequest Destination must be '{sloUrl}'.");
        RuleFor(m => m.Request.NotOnOrAfter).Must(n => n == null || timeProvider.GetUtcNow() < n.Value.Add(skew)).WithMessage("[OIO-GE-01] LogoutRequest NotOnOrAfter has passed.");
        // The IdP sends the request through the browser as soon as it issues it; an older one may be a replay.
        RuleFor(m => m.Request.IssueInstant).NotNull().WithMessage("LogoutRequest must contain an IssueInstant.")
            .Must(i => timeProvider.GetUtcNow().WithinAllowedPeriod(i!.Value, i.Value.Add(MaximumAge), skew))
            .WithMessage($"[OIO-GE-01] LogoutRequest IssueInstant is in the future or more than {MaximumAge.TotalMinutes} minutes old.");
        RuleFor(m => m.Request.Item).NotNull().WithMessage("LogoutRequest must contain a NameID.")
            .Must(i => i is not EncryptedElement).WithMessage("[OIO-IDP-24] NameID in LogoutRequest must not be encrypted.")
            .Must(i => i is NameID).WithMessage("LogoutRequest must identify the subject with a NameID.")
            .Must((m, i) => m.LoggedInSubject == null || ((NameID)i).Value == m.LoggedInSubject.Value && ((NameID)i).Format == m.LoggedInSubject.Format)
            .WithMessage("[OIO-SP-23] LogoutRequest NameID does not match the logged-in subject.");
        RuleFor(m => m.LogoutParameters.RelayState).Must(PendingRelayState.IsWithinLimit)
            .WithMessage($"[SAMLBind 3.4.3] RelayState must not exceed {PendingRelayState.MaxLength} bytes.");
    }
}
