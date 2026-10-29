using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class EncryptionMethod
    {
        public const string ELEMENT_NAME = "EncryptionMethod";

        private string algorithmField;
        private XmlNode[] anyField;
        private string keySizeField;

        private byte[] oAEPparamsField;

        [XmlElement(DataType="integer")]
        public string KeySize
        {
            get { return keySizeField; }
            set { keySizeField = value; }
        }

        [XmlElement(DataType="base64Binary")]
        public byte[] OAEPparams
        {
            get { return oAEPparamsField; }
            set { oAEPparamsField = value; }
        }

        [XmlText]
        [XmlAnyElement]
        public XmlNode[] Any
        {
            get { return anyField; }
            set { anyField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Algorithm
        {
            get { return algorithmField; }
            set { algorithmField = value; }
        }
    }
}