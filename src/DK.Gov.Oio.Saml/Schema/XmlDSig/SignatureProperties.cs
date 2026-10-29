using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class SignatureProperties
    {
        public const string ELEMENT_NAME = "SignatureProperties";

        private string idField;
        private SignatureProperty[] signaturePropertyField;

        [XmlElement("SignatureProperty")]
        public SignatureProperty[] SignatureProperty
        {
            get { return signaturePropertyField; }
            set { signaturePropertyField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }
    }
}