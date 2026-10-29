using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace = Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class EncryptedAssertion : EncryptedElement
    {
        public new const string ELEMENT_NAME = "EncryptedAssertion";
    }
}