using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XENC, IsNullable=false)]
    public class EncryptionProperties
    {
        public const string ELEMENT_NAME = "EncryptionProperties";

        private EncryptionProperty[] encryptionPropertyField;

        private string idField;

        [XmlElement("EncryptionProperty")]
        public EncryptionProperty[] EncryptionProperty
        {
            get { return encryptionPropertyField; }
            set { encryptionPropertyField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }
    }
}