using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Metadata;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [XmlInclude(typeof(RequestedAttribute))]
    [Serializable]    
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(SamlAttribute.ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class SamlAttribute
    {
        public const string NAMEFORMAT_URI = "urn:oasis:names:tc:SAML:2.0:attrname-format:uri";
        
        public const string NAMEFORMAT_BASIC = "urn:oasis:names:tc:SAML:2.0:attrname-format:basic";

        public const string ELEMENT_NAME = "Attribute";

        private XmlAttribute[] anyAttrField;
        private string[] attributeValueField;
        private string friendlyNameField;

        private string nameField;

        private string nameFormatField;

        [XmlElement("AttributeValue", IsNullable=true)]
        public string[] AttributeValue
        {
            get { return attributeValueField; }
            set { attributeValueField = value; }
        }

        [XmlAttribute]
        public string Name
        {
            get { return nameField; }
            set { nameField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string NameFormat
        {
            get { return nameFormatField; }
            set { nameFormatField = value; }
        }

        [XmlAttribute]
        public string FriendlyName
        {
            get { return friendlyNameField; }
            set { friendlyNameField = value; }
        }

        [XmlAnyAttribute]
        public XmlAttribute[] AnyAttr
        {
            get { return anyAttrField; }
            set { anyAttrField = value; }
        }
    }
}