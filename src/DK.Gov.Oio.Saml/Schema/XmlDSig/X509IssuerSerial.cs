using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    public class X509IssuerSerial
    {
        public const string ELEMENT_NAME = "X509IssuerSerial";

        private string x509IssuerNameField;

        private string x509SerialNumberField;

        public string X509IssuerName
        {
            get { return x509IssuerNameField; }
            set { x509IssuerNameField = value; }
        }

        [XmlElement(DataType="integer")]
        public string X509SerialNumber
        {
            get { return x509SerialNumberField; }
            set { x509SerialNumberField = value; }
        }
    }
}