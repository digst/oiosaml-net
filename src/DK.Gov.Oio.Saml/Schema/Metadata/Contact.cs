using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.METADATA, IsNullable = false)]
    public class Contact
    {
        public Contact() {}

        public Contact(ContactType contactType)
        {
            contactTypeField = contactType;
        }

        public const string ELEMENT_NAME = "ContactPerson";

        private XmlAttribute[] anyAttrField;
        private string companyField;
        private ContactType contactTypeField;
        private string[] emailAddressField;
        private ExtensionsType1 extensionsField;

        private string givenNameField;

        private string surNameField;

        private string[] telephoneNumberField;

        public ExtensionsType1 Extensions
        {
            get { return extensionsField; }
            set { extensionsField = value; }
        }

        public string Company
        {
            get { return companyField; }
            set { companyField = value; }
        }

        public string GivenName
        {
            get { return givenNameField; }
            set { givenNameField = value; }
        }

        public string SurName
        {
            get { return surNameField; }
            set { surNameField = value; }
        }

        [XmlElement("EmailAddress", DataType="anyURI")]
        public string[] EmailAddress
        {
            get { return emailAddressField; }
            set { emailAddressField = value; }
        }

        [XmlElement("TelephoneNumber")]
        public string[] TelephoneNumber
        {
            get { return telephoneNumberField; }
            set { telephoneNumberField = value; }
        }

        [XmlAttribute]
        public ContactType contactType
        {
            get { return contactTypeField; }
            set { contactTypeField = value; }
        }

        [XmlAnyAttribute]
        public XmlAttribute[] AnyAttr
        {
            get { return anyAttrField; }
            set { anyAttrField = value; }
        }
    }
}