using System.Threading;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Utils;
using DK.Gov.Oio.Saml.Validation.Model;
using FluentValidation;
using FluentValidation.Results;

namespace DK.Gov.Oio.Saml.Validation;

internal class SamlAuthnResponseValidator : AbstractValidator<SamlAuthnResponseValidationModel>
{
    private readonly ResponseEnvelopeValidator _envelope;

    public SamlAuthnResponseValidator(SamlAssertionValidator assertionValidator, OioSamlOptions options, IIdentityProviderCertificateStore certificateStore)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;
        _envelope = new ResponseEnvelopeValidator(options);

        Include(_envelope);
        RuleFor(m => m.Assertion).NotNull().WithMessage("Response contains no decrypted Assertion.");
        RuleFor(m => m.Assertion!.Issuer).NotNull().WithMessage("[OIO-IDP-14] Assertion must contain an Issuer.");
        RuleFor(m => m.Assertion!.Issuer.Value).NotEmpty().WithMessage("[OIO-IDP-14] Assertion must contain an Issuer.")
            .Equal(options.IdentityProvider.EntityId)
            .WithMessage(m => $"Assertion issuer '{m.Assertion!.Issuer.Value}' is not the configured identity provider '{options.IdentityProvider.EntityId}'.");
        RuleFor(m => m.Response.GetIssuerValue()).Must((m, issuer) => issuer == null || issuer == m.Assertion!.Issuer.Value)
            .WithMessage("Response Issuer does not match the Assertion Issuer.");
        RuleFor(m => m).CustomAsync(async (m, ctx, cancellationToken) =>
        {
            var trusted = await certificateStore.GetTrustedKeysAsync(cancellationToken);
            if (trusted.Keys is not { } keys) { ctx.AddFailure(trusted.Error); return; }
            if (XmlSignatureUtils.IsSigned(m.Response) && XmlSignatureUtils.Verify(m.Response, keys) is { } responseError)
                ctx.AddFailure($"Invalid Response signature: {responseError}");
            if (XmlSignatureUtils.Verify(m.AssertionXml!, keys) is { } assertionError)
                ctx.AddFailure($"[OIO-IDP-12] Invalid Assertion signature: {assertionError}");
        });
        RuleFor(m => new SamlAssertionValidationModel { Assertion = m.Assertion!, AuthnRequest = m.AuthnRequest! }).SetValidator(assertionValidator);
    }

    // The checks that need no decryption. They run first, so an unsolicited or misdirected response is refused
    // before the SP's private key is used on it.
    public Task<ValidationResult> ValidateBeforeDecryptionAsync(SamlAuthnResponseValidationModel model, CancellationToken cancellationToken = default) =>
        _envelope.ValidateAsync(model, cancellationToken);

    private sealed class ResponseEnvelopeValidator : AbstractValidator<SamlAuthnResponseValidationModel>
    {
        public ResponseEnvelopeValidator(OioSamlOptions options)
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;
            var acsUrl = options.ServiceProvider.GetAssertionConsumerServiceUrl();

            RuleFor(m => m.AuthnRequest).NotNull().WithMessage("No pending AuthnRequest found in session. Cannot validate response.");
            RuleFor(m => m.Response.GetAttribute("InResponseTo")).Must((m, v) => v == m.AuthnRequest!.ID)
                .WithMessage(m => $"InResponseTo '{m.Response.GetAttribute("InResponseTo")}' does not match the pending AuthnRequest '{m.AuthnRequest!.ID}'.");
            RuleFor(m => m.Response.GetAttribute("Destination")).Equal(acsUrl).WithMessage($"Response Destination must be the assertion consumer service URL '{acsUrl}'.");
            RuleFor(m => m.Response.GetIssuerValue()).Must(i => i == null || i.Length <= 256).WithMessage("[OIO-GE-03] Issuer must not exceed 256 characters.");
            RuleFor(m => m.Response.GetStatus()).NotNull().WithMessage("Response must contain a Status.")
                .Must(s => s!.IsSuccess()).WithMessage(m => m.Response.GetStatus()!.GetErrorMessage()).WithState(m => m.Response.GetStatus()!);
            RuleFor(m => m.Response.GetChildren("Assertion").Count).Equal(0).WithMessage("[OIO-IDP-13] Assertions must be encrypted; a plaintext Assertion is not accepted.");
            RuleFor(m => m.Response.GetChildren("EncryptedAssertion").Count).Equal(1).WithMessage("[OIO-IDP-11] Response must contain exactly one EncryptedAssertion.");
        }
    }
}
