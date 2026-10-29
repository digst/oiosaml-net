using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class AttributeQuery : SubjectQueryAbstract
    {
        public new const string ELEMENT_NAME = "AttributeQuery";

        private SamlAttribute[] samlAttributeField;

        [XmlElement("Attribute", Namespace=Saml20Constants.ASSERTION)]
        public SamlAttribute[] SamlAttribute
        {
            get { return samlAttributeField; }
            set { samlAttributeField = value; }
        }
    }
}