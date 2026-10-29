using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class Evidence
    {
        private ItemsChoiceType6[] itemsElementNameField;
        private object[] itemsField;

        public const string ELEMENT_NAME = "Evidence";

        [XmlElement("Assertion", typeof (Assertion))]
        [XmlElement("AssertionIDRef", typeof (string), DataType="NCName")]
        [XmlElement("AssertionURIRef", typeof (string), DataType="anyURI")]
        [XmlElement("EncryptedAssertion", typeof (EncryptedElement))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType6[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }
    }
}