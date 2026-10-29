using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class Signature 
    {
        public const string ELEMENT_NAME = "Signature";

        private string idField;
        private KeyInfo keyInfoField;

        private ObjectType[] objectField;
        private SignatureValue signatureValueField;
        private SignedInfo signedInfoField;

        public SignedInfo SignedInfo
        {
            get { return signedInfoField; }
            set { signedInfoField = value; }
        }

        public SignatureValue SignatureValue
        {
            get { return signatureValueField; }
            set { signatureValueField = value; }
        }

        public KeyInfo KeyInfo
        {
            get { return keyInfoField; }
            set { keyInfoField = value; }
        }

        [XmlElement("Object")]
        public ObjectType[] Object
        {
            get { return objectField; }
            set { objectField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }
    }
}