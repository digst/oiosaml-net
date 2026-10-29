using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [XmlInclude(typeof (EncryptedKey))]
    [XmlInclude(typeof (EncryptedData))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    public abstract class Encrypted
    {
        private CipherData cipherDataField;
        private string encodingField;
        private EncryptionMethod encryptionMethodField;

        private EncryptionProperties encryptionPropertiesField;

        private string idField;
        private KeyInfo keyInfoField;

        private string mimeTypeField;
        private string typeField;

        public EncryptionMethod EncryptionMethod
        {
            get { return encryptionMethodField; }
            set { encryptionMethodField = value; }
        }

        [XmlElement(Namespace=Saml20Constants.XMLDSIG)]
        public KeyInfo KeyInfo
        {
            get { return keyInfoField; }
            set { keyInfoField = value; }
        }

        public CipherData CipherData
        {
            get { return cipherDataField; }
            set { cipherDataField = value; }
        }

        public EncryptionProperties EncryptionProperties
        {
            get { return encryptionPropertiesField; }
            set { encryptionPropertiesField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Type
        {
            get { return typeField; }
            set { typeField = value; }
        }

        [XmlAttribute]
        public string MimeType
        {
            get { return mimeTypeField; }
            set { mimeTypeField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Encoding
        {
            get { return encodingField; }
            set { encodingField = value; }
        }
    }
}