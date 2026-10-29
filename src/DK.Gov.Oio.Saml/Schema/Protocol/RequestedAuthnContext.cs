using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class RequestedAuthnContext
    {
        public const string ELEMENT_NAME = "RequestedAuthnContext";

        private AuthnContextComparisonType comparisonField;

        private bool comparisonFieldSpecified;
        private ItemsChoiceType7[] itemsElementNameField;
        private string[] itemsField;

        [XmlElement("AuthnContextClassRef", typeof (string), Namespace=Saml20Constants.ASSERTION,
            DataType="anyURI")]
        [XmlElement("AuthnContextDeclRef", typeof (string), Namespace=Saml20Constants.ASSERTION,
            DataType="anyURI")]
        [XmlChoiceIdentifier("ItemsElementName")]
        public string[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType7[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }

        [XmlAttribute]
        public AuthnContextComparisonType Comparison
        {
            get { return comparisonField; }
            set { comparisonField = value; }
        }

        [XmlIgnore]
        public bool ComparisonSpecified
        {
            get { return comparisonFieldSpecified; }
            set { comparisonFieldSpecified = value; }
        }
    }
}