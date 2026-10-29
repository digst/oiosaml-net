using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class SignatureProperty
    {
        public const string ELEMENT_NAME = "SignatureProperty";

        private string idField;
        private XmlElement[] itemsField;

        private string targetField;
        private string[] textField;

        [XmlAnyElement]
        public XmlElement[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlText]
        public string[] Text
        {
            get { return textField; }
            set { textField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Target
        {
            get { return targetField; }
            set { targetField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }
    }
}