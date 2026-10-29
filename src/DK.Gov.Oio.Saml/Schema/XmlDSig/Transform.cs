using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class Transform
    {
        public const string ELEMENT_NAME = "Transform";

        private string algorithmField;
        private object[] itemsField;

        private string[] textField;

        [XmlAnyElement]
        [XmlElement("XPath", typeof (string))]
        public object[] Items
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
        public string Algorithm
        {
            get { return algorithmField; }
            set { algorithmField = value; }
        }
    }
}