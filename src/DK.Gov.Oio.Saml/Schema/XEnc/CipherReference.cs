using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XENC, IsNullable=false)]
    public class CipherReference
    {
        public const string ELEMENT_NAME = "CipherReference";

        private TransformsType1 itemField;

        private string uRIField;

        [XmlElement("Transforms")]
        public TransformsType1 Item
        {
            get { return itemField; }
            set { itemField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string URI
        {
            get { return uRIField; }
            set { uRIField = value; }
        }
    }
}