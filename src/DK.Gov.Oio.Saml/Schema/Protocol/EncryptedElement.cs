using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XEnc;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    [XmlInclude(typeof(EncryptedAssertion))]
    public class EncryptedElement
    {
        public const string ELEMENT_NAME = "EncryptedElement";

        private EncryptedData encryptedDataField;
        private EncryptedKey[] encryptedKeyField;

        [XmlElement("EncryptedData", Namespace=Saml20Constants.XENC)]
        public EncryptedData encryptedData
        {
            get { return encryptedDataField; }
            set { encryptedDataField = value; }
        }

        [XmlElement("EncryptedKey", Namespace=Saml20Constants.XENC)]
        public EncryptedKey[] encryptedKey
        {
            get { return encryptedKeyField; }
            set { encryptedKeyField = value; }
        }        
    }
}