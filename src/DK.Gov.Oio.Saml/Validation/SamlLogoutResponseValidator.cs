using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Validation.Model;
using FluentValidation;

namespace DK.Gov.Oio.Saml.Validation;

internal class SamlLogoutResponseValidator : AbstractValidator<LogoutResponseModel>
{
    public SamlLogoutResponseValidator(OioSamlOptions options, IIdentityProviderCertificateStore certificateStore)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;
        var sloUrl = options.ServiceProvider.GetSingleLogoutServiceUrl();

        RuleFor(m => m.Response.Issuer).NotNull().WithMessage("LogoutResponse must contain an Issuer.");
        RuleFor(m => m.Response.Issuer.Value).NotEmpty().WithMessage("LogoutResponse must contain an Issuer.")
            .Must(v => v.Length <= 256).WithMessage("[OIO-GE-03] Issuer must not exceed 256 characters.")
            .Equal(options.IdentityProvider.EntityId).WithMessage(m => $"Issuer '{m.Response.Issuer.Value}' is not the configured identity provider '{options.IdentityProvider.EntityId}'.");
        RuleFor(m => m).CustomAsync(async (m, ctx, cancellationToken) =>
        {
            var trusted = await certificateStore.GetTrustedKeysAsync(cancellationToken);
            if ((trusted.Error ?? SamlMessageSignature.Verify(m.LogoutParameters, m.RawXml, trusted.Keys!)) is { } failure)
                ctx.AddFailure($"[OIO-IDP-26] Invalid LogoutResponse signature: {failure}");
        });
        RuleFor(m => m.Response.Destination).Equal(sloUrl).WithMessage($"LogoutResponse Destination must be '{sloUrl}'.");
        RuleFor(m => m.Response.InResponseTo).NotEmpty().WithMessage("LogoutResponse must contain InResponseTo.")
            .Must((m, v) => v == m.ExpectedInResponseTo).WithMessage("LogoutResponse InResponseTo does not match a LogoutRequest issued by this service provider.");
        RuleFor(m => m.Response.Status).NotNull().WithMessage("LogoutResponse must contain a Status.")
            .Must(s => s.IsSuccess()).WithMessage(m => $"[OIO-IDP-27] IdP did not terminate the session: {m.Response.Status.GetErrorMessage()}").WithState(m => m.Response.Status);
    }
}
