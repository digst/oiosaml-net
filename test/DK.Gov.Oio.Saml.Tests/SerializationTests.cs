using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Tests;

public class SerializationTests
{
    private readonly XmlElement _root = Root(Serialization.SerializeToXmlString(TestFederation.AuthnRequest()));

    [Fact]
    public void RootIsWrittenWithoutPrefix() => Assert.Equal("", _root.Prefix);

    [Fact]
    public void RootDeclaresItsNamespaceAsDefault() => Assert.Equal(Saml20Constants.PROTOCOL, _root.GetAttribute("xmlns"));

    [Fact]
    public void NoSchemaInstanceNamespaceIsDeclared() => Assert.False(_root.HasAttribute("xmlns:xsi"));

    private static XmlElement Root(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        return document.DocumentElement!;
    }
}
