using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class KeyValue
    {
        public const string ELEMENT_NAME = "KeyValue";

        private object itemField;

        private string[] textField;

        [XmlAnyElement]
        [XmlElement("RSAKeyValue", typeof (RSAKeyValue))]
        public object Item
        {
            get { return itemField; }
            set { itemField = value; }
        }

        [XmlText]
        public string[] Text
        {
            get { return textField; }
            set { textField = value; }
        }
    }
}