using DK.Gov.Oio.Saml.Configuration;

namespace DK.Gov.Oio.Saml.Tests;

// The supported API outside the generated Schema classes. A type added here is a promise kept until the next major version;
// implementation types are internal and visible to the integration assemblies and tests only.
public class PublicApiTests
{
    private static readonly string[] Expected =
    [
        "Certificates.CertificateStatus",
        "Certificates.CertificateValidationResult",
        "Certificates.ICertificateValidator",
        "Certificates.IIdentityProviderCertificateStore",
        "Certificates.IdentityProviderCertificateRefresh",
        "Certificates.RevocationCertificateValidator",
        "Certificates.ServiceProviderCertificateExpiry",
        "Configuration.AppSwitchReturnUrl",
        "Configuration.CertificateRevocationOptions",
        "Configuration.CertificateSource",
        "Configuration.IdentityProvider",
        "Configuration.IdentityProviderEndpointService",
        "Configuration.IdentityProviderMetadata",
        "Configuration.LevelOfAssuranceOptions",
        "Configuration.LoginOptions",
        "Configuration.OioSamlOptions",
        "Configuration.Platform",
        "Configuration.ServiceProviderOptions",
        "Configuration.SessionOptions",
        "Constants.ClaimsConstants",
        "Constants.LoAValues",
        "Constants.OioSaml3Profiles",
        "Constants.OioSaml4AttributeProfiles",
        "Constants.OioSaml4Constants",
        "Constants.Saml20Constants",
        "Constants.Saml20Constants.AttributeNameFormats",
        "Constants.Saml20Constants.NameIdentifierFormats",
        "Constants.Saml20Constants.ProtocolBindings",
        "Constants.Saml20Constants.Reasons",
        "Constants.Saml20Constants.StatusCodes",
        "Constants.Saml20Constants.SubjectConfirmationMethods",
        "DependencyInjection.OioSamlCoreServiceCollectionExtensions",
        "Exceptions.Saml20Exception",
        "Exceptions.Saml20FormatException",
        "Exceptions.Saml20StatusException",
        "Exceptions.Saml20ValidationException",
        "Interfaces.IAssertionReplayCache",
        "Interfaces.IAuthnRequestParameters",
        "Interfaces.IAuthnRequestProcessor",
        "Interfaces.IIdentityProviderMetadataService",
        "Interfaces.ILogoutParameters",
        "Interfaces.ISaml20LoginService",
        "Interfaces.ISaml20LogoutService",
        "Interfaces.ISamlLoginResponse",
        "Interfaces.ISecurityContext",
        "Interfaces.NoopAuthnRequestProcessor",
        "Saml20AuthnRequest",
        "Services.DistributedCacheAssertionReplayCache",
        "Services.IdentityProviderMetadataService",
        "Services.InMemoryAssertionReplayCache",
        "Services.LoginResult",
        "Services.PendingRelayState",
        "Services.SessionSecurityContext",
        "Services.SignedLogoutRequest",
        "Services.SignedLogoutResponse",
    ];

    [Fact]
    public void PublicTypes_AreTheSupportedApi() =>
        Assert.Equal(Expected, typeof(OioSamlOptions).Assembly.GetExportedTypes()
            .Where(t => !t.Namespace!.StartsWith("DK.Gov.Oio.Saml.Schema"))
            .Select(t => t.FullName!["DK.Gov.Oio.Saml.".Length..].Replace('+', '.'))
            .Order(StringComparer.Ordinal));
}
