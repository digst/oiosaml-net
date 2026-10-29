using System.Security.Cryptography.X509Certificates;
using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Logging;
using DK.Gov.Oio.Saml.Schema.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace DK.Gov.Oio.Saml.Tests.Builders;

public static class TestFederation
{
    public const string IdpEntityId = "https://idp.test";
    public const string SpEntityId = "https://sp.test";
    public const string Server = "https://sp.test";
    public const string AcsUrl = "https://sp.test/auth/AssertionConsumerService";
    public const string SloUrl = "https://sp.test/auth/logout";
    public const string IdpSsoUrl = "https://idp.test/sso";
    public const string IdpSloRedirectUrl = "https://idp.test/slo";
    public const string IdpSloPostUrl = "https://idp.test/slo-post";
    public const string AuthnRequestId = "id0123456789abcdef";

    // A response to the pending request whose Status nests StatusCode elements, which XmlSerializer reads recursively.
    // Without the depth limit this overflows the stack and the test process crashes instead of failing.
    internal static string DeeplyNested(string root = "Response", int depth = 100_000) =>
        $"<samlp:{root} xmlns:samlp=\"{Saml20Constants.PROTOCOL}\" ID=\"_x\" Version=\"2.0\" IssueInstant=\"2026-01-01T00:00:00Z\" InResponseTo=\"{AuthnRequestId}\" Destination=\"{AcsUrl}\"><samlp:Status>"
        + string.Concat(Enumerable.Repeat("<samlp:StatusCode Value=\"x\">", depth)) + string.Concat(Enumerable.Repeat("</samlp:StatusCode>", depth))
        + $"</samlp:Status></samlp:{root}>";

    public static OioSamlOptions Options()
    {
        var options = new OioSamlOptions
        {
            ServiceProvider = new ServiceProviderOptions { EntityId = SpEntityId, Server = Server },
            IdentityProvider = new IdentityProvider { EntityId = IdpEntityId, MetadataFile = "idp-metadata/test-idp.xml" },
        };
        options.ServiceProvider.SetCertificate(TestCertificates.Sp);
        return options;
    }

    // The runtime-generated IdP certificates are self-signed and name no revocation source, so only their validity period is checked.
    internal static AuditLog AuditLog(FakeLogCollector? collector = null) =>
        new(collector == null ? NullLoggerFactory.Instance : new LoggerFactory([new FakeLoggerProvider(collector)]));

    public static IIdentityProviderCertificateStore CertificateStore(params X509Certificate2[] idpSigningCertificates)
    {
        var options = Options();
        options.CertificateRevocation.Enabled = false;
        return new IdentityProviderCertificateStore(options, MetadataService(idpSigningCertificates),
            new RevocationCertificateValidator(options, TimeProvider.System), TimeProvider.System);
    }

    public static IIdentityProviderMetadataService MetadataService(params X509Certificate2[] idpSigningCertificates)
    {
        var service = new Mock<IIdentityProviderMetadataService>();
        service.Setup(x => x.GetMetadata()).Returns(new IdentityProviderMetadata(IdpEntityId, new(Saml20Constants.ProtocolBindings.HTTP_Redirect, IdpSsoUrl))
        {
            SigningCertificates = idpSigningCertificates.Select(c => c.Base64()).ToList(),
            SingleLogoutServices =
            [
                new(Saml20Constants.ProtocolBindings.HTTP_Redirect, IdpSloRedirectUrl),
                new(Saml20Constants.ProtocolBindings.HTTP_Post, IdpSloPostUrl),
            ],
        });
        return service.Object;
    }

    public static AuthnRequest AuthnRequest(bool forceAuthn = false, string? requestedClassRef = null, params string[] requestedProfiles) => new()
    {
        ID = AuthnRequestId,
        IssueInstant = DateTime.UtcNow.AddSeconds(-30),
        ForceAuthn = forceAuthn,
        RequestedAuthnContext = requestedClassRef == null ? null : new RequestedAuthnContext { Items = [requestedClassRef], ItemsElementName = [ItemsChoiceType7.AuthnContextClassRef] },
        Extensions = requestedProfiles.Length == 0 ? null : new DK.Gov.Oio.Saml.Schema.Protocol.Extensions { Any = [RequestedAttributeProfiles(requestedProfiles)] },
    };

    private static System.Xml.XmlElement RequestedAttributeProfiles(string[] profiles)
    {
        const string Namespace = "https://data.gov.dk/eid/saml/extensions";
        var doc = new System.Xml.XmlDocument();
        var element = doc.CreateElement("oiosaml", "RequestedAttributeProfiles", Namespace);
        foreach (var profile in profiles)
        {
            var child = doc.CreateElement("oiosaml", "Profile", Namespace);
            child.InnerText = profile;
            element.AppendChild(child);
        }
        return element;
    }
}

public class TestAuthnRequestParameters : IAuthnRequestParameters
{
    public bool? ForceAuthn => null;
    public bool? IsPassive => null;
    public string? Loa => null;
    public string? AppSwitchPlatform => null;
    public string? Profile => null;
    public string[] AttributeProfiles => [];
    public string? RelayState { get; set; }
    public string? ReturnUrl { get; set; }
}

public class TestLoginResponse(string? loginResponse) : ISamlLoginResponse
{
    public string? LoginResponse { get; set; } = loginResponse;
    public string? RelayState { get; set; }
}

public class TestLogoutParameters : ILogoutParameters
{
    public string Binding { get; set; } = Saml20Constants.ProtocolBindings.HTTP_Post;
    public string? LogoutRequest { get; set; }
    public string? LogoutResponse { get; set; }
    public string? RelayState { get; set; }
    public string? SigAlg { get; set; }
    public string? Signature { get; set; }
    public string? RawQuery { get; set; }
}
