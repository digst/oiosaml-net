using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [XmlInclude(typeof(IndexedEndpoint))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.METADATA, IsNullable = false)]
    public class Endpoint {
        public const string ELEMENT_NAME = "SingleLogoutService";

        private XmlElement[] anyField;
        
        private string bindingField;
        
        private string locationField;
        
        private string responseLocationField;
        
        private XmlAttribute[] anyAttrField;

        [XmlAnyElement]
        public XmlElement[] Any {
            get {
                return anyField;
            }
            set {
                anyField = value;
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Binding {
            get {
                return bindingField;
            }
            set {
                bindingField = value;
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Location {
            get {
                return locationField;
            }
            set {
                locationField = value;
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string ResponseLocation {
            get {
                return responseLocationField;
            }
            set {
                responseLocationField = value;
            }
        }

        [XmlAnyAttribute]
        public XmlAttribute[] AnyAttr {
            get {
                return anyAttrField;
            }
            set {
                anyAttrField = value;
            }
        }
    }
}