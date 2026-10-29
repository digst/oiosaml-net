using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class AuthnAuthorityDescriptor : RoleDescriptor {
        public new const string ELEMENT_NAME = "AuthnAuthorityDescriptor";

        private Endpoint[] authnQueryServiceField;
        
        private Endpoint[] assertionIDRequestServiceField;
        
        private string[] nameIDFormatField;

        [XmlElement("AuthnQueryService")]
        public Endpoint[] AuthnQueryService {
            get {
                return authnQueryServiceField;
            }
            set {
                authnQueryServiceField = value;
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