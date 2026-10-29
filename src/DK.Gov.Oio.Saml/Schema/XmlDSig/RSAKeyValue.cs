using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class RSAKeyValue
    {
        public const string ELEMENT_NAME = "RSAKeyValue";

        private byte[] exponentField;
        private byte[] modulusField;

        [XmlElement(DataType="base64Binary")]
        public byte[] Modulus
        {
            get { return modulusField; }
            set { modulusField = value; }
        }

        [XmlElement(DataType="base64Binary")]
        public byte[] Exponent
        {
            get { return exponentField; }
            set { exponentField = value; }
        }
    }
}