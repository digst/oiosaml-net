using System.Xml;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Tests;

public class SecureXmlTests
{
    [Fact]
    public void RejectsDocumentTypeDefinitions()
    {
        const string xml = """<!DOCTYPE a [<!ENTITY x "boom">]><a>&x;</a>""";

        Assert.Throws<XmlException>(() => SecureXml.Parse(xml));
    }

    [Fact]
    public void PreservesWhitespaceSoSignaturesVerify()
    {
        var doc = SecureXml.Parse("<a>\n  <b/>\n</a>");

        Assert.True(doc.PreserveWhitespace);
        Assert.Equal("<a>\n  <b />\n</a>", doc.OuterXml);
    }
}
