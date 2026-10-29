using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class ObjectType {
        public const string ELEMENT_NAME = "Object";
        
        private XmlNode[] anyField;
        
        private string idField;
        
        private string mimeTypeField;
        
        private string encodingField;

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

        [XmlAttribute(DataType="ID")]
        public string Id {
            get {
                return idField;
            }
            set {
                idField = value;
            }
        }

        [XmlAttribute]
        public string MimeType {
            get {
                return mimeTypeField;
            }
            set {
                mimeTypeField = value;
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Encoding {
            get {
                return encodingField;
            }
            set {
                encodingField = value;
            }
        }
    }
}