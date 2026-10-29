using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    // The library does not use Advice, so its content is not validated.
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]    
    public class Advice
    {
        public const string ELEMENT_NAME = "Advice";

        private ItemsChoiceType4[] itemsElementNameField;
        private object[] itemsField;

        [XmlAnyElement]
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
        public ItemsChoiceType4[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }
    }
}