using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DK.Gov.Oio.Saml.Utils;

internal static class Serialization
{
    // The root's own namespace as the default, which also keeps xsi/xsd declarations out. Mapping "" to no namespace instead
    // makes .NET Framework write the root with a generated prefix (<q1:AuthnRequest>), which NemLog-in refuses.
    private static XmlSerializerNamespaces NamespacesFor(Type type)
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", type.GetCustomAttribute<XmlRootAttribute>()?.Namespace ?? type.GetCustomAttribute<XmlTypeAttribute>()?.Namespace ?? "");
        return namespaces;
    }

    public static void Serialize<T>(T item, Stream stream)
    {
        new XmlSerializer(typeof(T)).Serialize(stream, item, NamespacesFor(typeof(T)));
        stream.Flush();
    }

    public static XmlElement Serialize<T>(T item, XmlDocument owner) where T : class
    {
        var xDoc = new XDocument();
        using (var writer = xDoc.CreateWriter())
            new XmlSerializer(typeof(T)).Serialize(writer, item, NamespacesFor(typeof(T)));
        var root = xDoc.Root ?? throw new XmlException($"Serialization of object type {typeof(T).Name} did not produce a root XML element.");
        using var reader = root.CreateReader();
        return (XmlElement)owner.ReadNode(reader)!;
    }

    public static XmlDocument Serialize<T>(T item)
    {
        using var stream = new MemoryStream();
        Serialize(item, stream);
        stream.Seek(0, SeekOrigin.Begin);
        var doc = new XmlDocument { XmlResolver = null };
        doc.Load(stream);
        return doc;
    }

    // Whitespace is preserved, as the document is signed after this.
    internal static XmlDocument ToXmlDocument<T>(T item)
    {
        var doc = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };
        doc.LoadXml(SerializeToXmlString(item));
        return doc;
    }

    public static string SerializeToXmlString<T>(T item)
    {
        using var stream = new MemoryStream();
        Serialize(item, stream);
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static T DeserializeFromXmlString<T>(string xml)
    {
        using var reader = SecureXml.CreateReader(xml);
        return Deserialize<T>(reader);
    }

    public static T Deserialize<T>(XmlReader reader) => (T)new XmlSerializer(typeof(T)).Deserialize(reader)!;
}
