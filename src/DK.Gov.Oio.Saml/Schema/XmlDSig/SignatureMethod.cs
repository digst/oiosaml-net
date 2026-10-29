using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class SignatureMethod {
        public const string ELEMENT_NAME = "SignatureMethod";

        private string hMACOutputLengthField;
        
        private XmlNode[] anyField;
        
        private string algorithmField;

        [XmlElement(DataType="integer")]
        public string HMACOutputLength {
            get {
                return hMACOutputLengthField;
            }
            set {
                hMACOutputLengthField = value;
            }
        }

        [XmlText]
        [XmlAnyElement]
        public XmlNode[] Any {
            get {
                return anyField;
            }
            set {
                anyField = value;
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Algorithm {
            get {
                return algorithmField;
            }
            set {
                algorithmField = value;
            }
        }
    }
}