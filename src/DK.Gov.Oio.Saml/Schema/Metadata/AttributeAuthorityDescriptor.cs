using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class AttributeAuthorityDescriptor : RoleDescriptor {
        public new const string ELEMENT_NAME = "AttributeAuthorityDescriptor";

        private Endpoint[] attributeServiceField;
        
        private Endpoint[] assertionIDRequestServiceField;
        
        private string[] nameIDFormatField;
        
        private string[] attributeProfileField;
        
        private Attribute[] attributeField;

        [XmlElement("AttributeService")]
        public Endpoint[] AttributeService {
            get {
                return attributeServiceField;
            }
            set {
                attributeServiceField = value;
            }
        }

        [XmlElement("AssertionIDRequestService")]
        public Endpoint[] AssertionIDRequestService {
            get {
                return assertionIDRequestServiceField;
            }
            set {
                assertionIDRequestServiceField = value;
            }
        }

        [XmlElement("NameIDFormat", DataType="anyURI")]
        public string[] NameIDFormat {
            get {
                return nameIDFormatField;
            }
            set {
                nameIDFormatField = value;
            }
        }

        [XmlElement("AttributeProfile", DataType="anyURI")]
        public string[] AttributeProfile {
            get {
                return attributeProfileField;
            }
            set {
                attributeProfileField = value;
            }
        }
    }
}