using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class X509Data
    {
        public const string ELEMENT_NAME = "X509Data";

        private ItemsChoiceType[] itemsElementNameField;
        private object[] itemsField;

        [XmlAnyElement]
        [XmlElement("X509CRL", typeof (byte[]), DataType="base64Binary")]
        [XmlElement("X509Certificate", typeof (byte[]), DataType="base64Binary")]
        [XmlElement("X509IssuerSerial", typeof (X509IssuerSerial))]
        [XmlElement("X509SKI", typeof (byte[]), DataType="base64Binary")]
        [XmlElement("X509SubjectName", typeof (string))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }
    }
}