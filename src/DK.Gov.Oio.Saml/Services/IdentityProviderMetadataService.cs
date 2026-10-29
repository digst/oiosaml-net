using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Services;

// Reads the file on every call, so a replaced metadata file (e.g. an IdP certificate rollover) takes effect without a restart.
public class IdentityProviderMetadataService(OioSamlOptions options) : IIdentityProviderMetadataService
{
    private static readonly XNamespace Md = "urn:oasis:names:tc:SAML:2.0:metadata";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    public IdentityProviderMetadata GetMetadata() => Read(options.IdentityProvider);

    internal static IdentityProviderMetadata Read(IdentityProvider identityProvider)
    {
        var file = identityProvider.GetMetadataFile();
        var xml = File.ReadAllText(file);
        SecureXml.CheckDepth(xml);
        XElement entityDescriptor;
        using (var reader = SecureXml.CreateReader(xml))
            entityDescriptor = XDocument.Load(reader).Root ?? throw Malformed(file, "has no root element");
        var entityId = entityDescriptor.Attribute("entityID")?.Value ?? throw Malformed(file, "has no entityID");
        if (entityId != identityProvider.EntityId)
            throw Malformed(file, $"is for entityID '{entityId}', but identityProvider:entityId is '{identityProvider.EntityId}'");
        var idpSsoDescriptor = entityDescriptor.Element(Md + "IDPSSODescriptor") ?? throw Malformed(file, "has no IDPSSODescriptor");
        var singleSignOnService = idpSsoDescriptor.Element(Md + "SingleSignOnService") ?? throw Malformed(file, "has no SingleSignOnService");

        return new IdentityProviderMetadata(entityId, Endpoint(file, singleSignOnService))
        {
            SigningCertificates = idpSsoDescriptor.Elements(Md + "KeyDescriptor")
                .Where(x => (string?)x.Attribute("use") is null or "signing")
                .SelectMany(x => x.Descendants(Ds + "X509Certificate"))
                .Select(x => x.Value)
                .ToList(),
            SingleLogoutServices = idpSsoDescriptor.Elements(Md + "SingleLogoutService").Select(x => Endpoint(file, x)).ToList(),
        };
    }

    private static IdentityProviderEndpointService Endpoint(string file, XElement element) => new(
        element.Attribute("Binding")?.Value ?? Saml20Constants.ProtocolBindings.HTTP_Redirect,
        element.Attribute("Location")?.Value ?? throw Malformed(file, $"has a {element.Name.LocalName} without Location"));

    private static InvalidOperationException Malformed(string file, string problem) => new($"IdP metadata file '{file}' {problem}.");
}
