using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class PGPData
    {
        public const string ELEMENT_NAME = "PGPData";

        private ItemsChoiceType1[] itemsElementNameField;
        private object[] itemsField;

        [XmlAnyElement]
        [XmlElement("PGPKeyID", typeof (byte[]), DataType="base64Binary")]
        [XmlElement("PGPKeyPacket", typeof (byte[]), DataType="base64Binary")]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType1[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }
    }
}