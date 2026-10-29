using System.IO;
using System.Xml;

namespace DK.Gov.Oio.Saml.Utils;

internal static class SecureXml
{
    // A SAML message or metadata nests about 10 levels. The DOM and XmlSerializer recurse once per level, and a stack
    // overflow ends the process, so a deeper document is refused by a forward-only pass before anything is built from it.
    public const int MaxDepth = 64;

    // [OIO-GE-02] DTDs are rejected. PreserveWhitespace is required for XML signatures to verify.
    public static readonly XmlReaderSettings ReaderSettings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

    public static XmlReader CreateReader(string xml) => XmlReader.Create(new StringReader(xml), ReaderSettings);

    public static XmlDocument Parse(string xml)
    {
        CheckDepth(xml);
        var doc = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };
        using var reader = CreateReader(xml);
        doc.Load(reader);
        return doc;
    }

    public static void CheckDepth(string xml)
    {
        using var reader = CreateReader(xml);
        while (reader.Read())
            if (reader.Depth > MaxDepth) throw new XmlException($"The XML is nested more than {MaxDepth} levels deep.");
    }
}
