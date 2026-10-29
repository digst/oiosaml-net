using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;
using DK.Gov.Oio.Saml.Tests.Builders;

namespace DK.Gov.Oio.Saml.Tests;

public class IdentityProviderMetadataServiceTests
{
    private const string Md = "urn:oasis:names:tc:SAML:2.0:metadata";
    private const string DevTest4 = "https://saml.test-devtest4-nemlog-in.dk";
    private const string DevTest4Metadata = "idp-metadata/test-devtest4-idp-metadata.xml";

    private static IdentityProviderMetadata Read(string entityId, string metadataFile) =>
        new IdentityProviderMetadataService(new OioSamlOptions { IdentityProvider = new IdentityProvider { EntityId = entityId, MetadataFile = metadataFile } }).GetMetadata();

    [Fact]
    public void ShouldGetMetadataForIdentityProvider()
    {
        var actual = Read(DevTest4, DevTest4Metadata);

        Assert.Equal(DevTest4, actual.EntityId);
        Assert.Equal(Saml20Constants.ProtocolBindings.HTTP_Redirect, actual.SingleSignOnService.Binding);
        Assert.Equal("https://test-devtest4-nemlog-in.dk/idp/saml/3.0/", actual.SingleSignOnService.Location);
        Assert.Equal(2, actual.SingleLogoutServices.Count);
        Assert.Equal(Saml20Constants.ProtocolBindings.HTTP_Redirect, actual.SingleLogoutServices[0].Binding);
        Assert.Equal("https://test-devtest4-nemlog-in.dk/idp/saml/3.0/", actual.SingleLogoutServices[0].Location);
        Assert.Equal(Saml20Constants.ProtocolBindings.HTTP_Post, actual.SingleLogoutServices[1].Binding);
        Assert.Equal("https://test-devtest4-nemlog-in.dk/idp/saml/3.0/", actual.SingleLogoutServices[1].Location);
        Assert.Single(actual.SigningCertificates);
    }

    [Fact]
    public void MetadataOfAnotherIdentityProvider_IsRefused() =>
        Assert.Contains($"is for entityID '{DevTest4}', but identityProvider:entityId is 'https://idp.test'",
            Assert.Throws<InvalidOperationException>(() => Read("https://idp.test", DevTest4Metadata)).Message);

    [Fact]
    public void KeyDescriptorWithoutUse_IsASigningCertificate() =>
        Assert.Equal(["dW5zcGVjaWZpZWQ=", "c2lnbmluZw=="], Read("https://keydescriptor-use.identity-provider.dk", "idp-metadata/keydescriptor-use.identity-provider.xml").SigningCertificates);

    [Fact]
    public void MetadataWithDocumentTypeDefinition_IsRefused()
    {
        var file = Path.Combine(Directory.CreateTempSubdirectory("oiosaml-metadata").FullName, "idp.xml");
        try
        {
            File.WriteAllText(file, $"<!DOCTYPE x [<!ENTITY e 'x'>]><EntityDescriptor xmlns='{Md}' entityID='https://idp.test'/>");
            Assert.Throws<System.Xml.XmlException>(() => Read("https://idp.test", file));
        }
        finally { Directory.Delete(Path.GetDirectoryName(file)!, true); }
    }

    [Fact]
    public void DeeplyNestedMetadata_IsRefused()
    {
        var file = Path.Combine(Directory.CreateTempSubdirectory("oiosaml-metadata").FullName, "idp.xml");
        try
        {
            File.WriteAllText(file, TestFederation.DeeplyNested());
            Assert.Contains("nested more than", Assert.Throws<System.Xml.XmlException>(() => Read("https://idp.test", file)).Message);
        }
        finally { Directory.Delete(Path.GetDirectoryName(file)!, true); }
    }

    private static Exception GetMetadataFrom(string metadata)
    {
        var file = Path.Combine(Directory.CreateTempSubdirectory("oiosaml-metadata").FullName, "idp.xml");
        try
        {
            File.WriteAllText(file, metadata);
            return Assert.Throws<InvalidOperationException>(() => Read("https://idp.test", file));
        }
        finally { Directory.Delete(Path.GetDirectoryName(file)!, true); }
    }

    public static TheoryData<string, string> MalformedMetadata => new()
    {
        { $"<EntityDescriptor xmlns='{Md}'/>", "has no entityID" },
        { $"<EntityDescriptor xmlns='{Md}' entityID='https://idp.test'/>", "has no IDPSSODescriptor" },
        { $"<EntityDescriptor xmlns='{Md}' entityID='https://idp.test'><IDPSSODescriptor/></EntityDescriptor>", "has no SingleSignOnService" },
        { $"<EntityDescriptor xmlns='{Md}' entityID='https://idp.test'><IDPSSODescriptor><SingleSignOnService/></IDPSSODescriptor></EntityDescriptor>", "has a SingleSignOnService without Location" },
        { $"<EntityDescriptor xmlns='{Md}' entityID='https://idp.test'><IDPSSODescriptor><SingleLogoutService/><SingleSignOnService Location='https://idp.test/sso'/></IDPSSODescriptor></EntityDescriptor>", "has a SingleLogoutService without Location" },
    };

    [Theory]
    [MemberData(nameof(MalformedMetadata))]
    public void MalformedMetadata_IsReportedWithTheProblem(string metadata, string problem) =>
        Assert.Contains(problem, GetMetadataFrom(metadata).Message);
}
