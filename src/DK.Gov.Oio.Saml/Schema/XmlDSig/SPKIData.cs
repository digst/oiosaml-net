using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class SPKIData
    {
        public const string ELEMENT_NAME = "SPKIData";

        private XmlElement anyField;
        private byte[][] sPKISexpField;

        [XmlElement("SPKISexp", DataType="base64Binary")]
        public byte[][] SPKISexp
        {
            get { return sPKISexpField; }
            set { sPKISexpField = value; }
        }

        [XmlAnyElement]
        public XmlElement Any
        {
            get { return anyField; }
            set { anyField = value; }
        }
    }
}