using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XENC, IsNullable=false)]
    public class EncryptedKey : Encrypted
    {
        public const string ELEMENT_NAME = "EncryptedKey";

        private string carriedKeyNameField;

        private string recipientField;
        private ReferenceList referenceListField;

        public ReferenceList ReferenceList
        {
            get { return referenceListField; }
            set { referenceListField = value; }
        }

        public string CarriedKeyName
        {
            get { return carriedKeyNameField; }
            set { carriedKeyNameField = value; }
        }

        [XmlAttribute]
        public string Recipient
        {
            get { return recipientField; }
            set { recipientField = value; }
        }
    }
}