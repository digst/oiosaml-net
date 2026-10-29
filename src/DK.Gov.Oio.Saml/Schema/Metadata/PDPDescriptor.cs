using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class PDPDescriptor : RoleDescriptor {
        public new const string ELEMENT_NAME = "Organization";

        private Endpoint[] authzServiceField;
        
        private Endpoint[] assertionIDRequestServiceField;
        
        private string[] nameIDFormatField;

        [XmlElement("AuthzService")]
        public Endpoint[] AuthzService {
            get {
                return authzServiceField;
            }
            set {
                authzServiceField = value;
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
    }
}