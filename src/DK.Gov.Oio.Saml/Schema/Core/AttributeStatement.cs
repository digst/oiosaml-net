using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class AttributeStatement : StatementAbstract
    {
        private object[] itemsField;

        public new const string ELEMENT_NAME = "AttributeStatement";

        [XmlElement("Attribute", typeof (SamlAttribute))]
        [XmlElement("EncryptedAttribute", typeof (EncryptedElement))]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }
    }
}