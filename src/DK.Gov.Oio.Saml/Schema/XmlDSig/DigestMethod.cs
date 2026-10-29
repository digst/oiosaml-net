using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class DigestMethod
    {
        public const string ELEMENT_NAME = "DigestMethod";

        private string algorithmField;
        private XmlNode[] anyField;

        [XmlText]
        [XmlAnyElement]
        public XmlNode[] Any
        {
            get { return anyField; }
            set { anyField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Algorithm
        {
            get { return algorithmField; }
            set { algorithmField = value; }
        }
    }
}