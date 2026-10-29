using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [GeneratedCode("xsd", "2.0.50727.42")]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class Scoping
    {
        public const string ELEMENT_NAME = "Scoping";

        private IDPList iDPListField;

        private string proxyCountField;
        private string[] requesterIDField;

        public IDPList IDPList
        {
            get { return iDPListField; }
            set { iDPListField = value; }
        }

        [XmlElement("RequesterID", DataType="anyURI")]
        public string[] RequesterID
        {
            get { return requesterIDField; }
            set { requesterIDField = value; }
        }

        [XmlAttribute(DataType="nonNegativeInteger")]
        public string ProxyCount
        {
            get { return proxyCountField; }
            set { proxyCountField = value; }
        }
    }
}