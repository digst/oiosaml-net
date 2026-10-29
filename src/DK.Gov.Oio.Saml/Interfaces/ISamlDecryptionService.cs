using System.Xml;

namespace DK.Gov.Oio.Saml.Interfaces;

internal interface ISamlDecryptionService
{
    XmlElement Decrypt(XmlElement encryptedAssertionNode);
}