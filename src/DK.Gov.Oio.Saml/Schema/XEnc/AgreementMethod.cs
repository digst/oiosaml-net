using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XENC, IsNullable=false)]
    public class AgreementMethod
    {
        public const string ELEMENT_NAME = "AgreementMethod";

        private string algorithmField;
        private XmlNode[] anyField;
        private byte[] kANonceField;

        private KeyInfo originatorKeyInfoField;

        private KeyInfo recipientKeyInfoField;

        [XmlElement("KA-Nonce", DataType="base64Binary")]
        public byte[] KANonce
        {
            get { return kANonceField; }
            set { kANonceField = value; }
        }

        [XmlText]
        [XmlAnyElement]
        public XmlNode[] Any
        {
            get { return anyField; }
            set { anyField = value; }
        }

        public KeyInfo OriginatorKeyInfo
        {
            get { return originatorKeyInfoField; }
            set { originatorKeyInfoField = value; }
        }

        public KeyInfo RecipientKeyInfo
        {
            get { return recipientKeyInfoField; }
            set { recipientKeyInfoField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Algorithm
        {
            get { return algorithmField; }
            set { algorithmField = value; }
        }
    }
}