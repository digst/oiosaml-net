using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XEnc;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class KeyDescriptor
    {
        public const string ELEMENT_NAME = "KeyDescriptor";

        private EncryptionMethod[] encryptionMethodField;
        private KeyInfo keyInfoField;

        private KeyTypes useField;

        private bool useFieldSpecified;

        [XmlElement(Namespace=Saml20Constants.XMLDSIG)]
        public KeyInfo KeyInfo
        {
            get { return keyInfoField; }
            set { keyInfoField = value; }
        }

        [XmlElement("EncryptionMethod")]
        public EncryptionMethod[] EncryptionMethod
        {
            get { return encryptionMethodField; }
            set { encryptionMethodField = value; }
        }

        [XmlAttribute]
        public KeyTypes use
        {
            get { return useField; }
            set { useField = value; }
        }

        [XmlIgnore]
        public bool useSpecified
        {
            get { return useFieldSpecified; }
            set { useFieldSpecified = value; }
        }
    }
}