using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class AuthnContext
    {
        private string[] authenticatingAuthorityField;
        private ItemsChoiceType5[] itemsElementNameField;
        private object[] itemsField;

        public const string ELEMENT_NAME = "AuthnContext";

        [XmlElement("AuthnContextClassRef", typeof (string), DataType="anyURI")]
        [XmlElement("AuthnContextDecl", typeof (object))]
        [XmlElement("AuthnContextDeclRef", typeof (string), DataType="anyURI")]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType5[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }

        [XmlElement("AuthenticatingAuthority", DataType="anyURI")]
        public string[] AuthenticatingAuthority
        {
            get { return authenticatingAuthorityField; }
            set { authenticatingAuthorityField = value; }
        }
    }
}