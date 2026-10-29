using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace = Saml20Constants.METADATA)]
    public class IDPSSODescriptor : SSODescriptor {
        public new const string ELEMENT_NAME = "IDPSSODescriptor";

        private Endpoint[] singleSignOnServiceField;
        
        private Endpoint[] nameIDMappingServiceField;
        
        private Endpoint[] assertionIDRequestServiceField;
        
        private string[] attributeProfileField;
        
        private SamlAttribute[] attributeField;
        
        private bool wantAuthnRequestsSignedField;
        
        private bool wantAuthnRequestsSignedFieldSpecified;

        [XmlElement("SingleSignOnService")]
        public Endpoint[] SingleSignOnService {
            get {
                return singleSignOnServiceField;
            }
            set {
                singleSignOnServiceField = value;
            }
        }

        [XmlElement("NameIDMappingService")]
        public Endpoint[] NameIDMappingService {
            get {
                return nameIDMappingServiceField;
            }
            set {
                nameIDMappingServiceField = value;
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

        [XmlElement("AttributeProfile", DataType="anyURI")]
        public string[] AttributeProfile {
            get {
                return attributeProfileField;
            }
            set {
                attributeProfileField = value;
            }
        }

        [XmlElement("Attribute", Namespace=Saml20Constants.ASSERTION)]
        public SamlAttribute[] Attributes    {
            get {
                return attributeField;
            }
            set {
                attributeField = value;
            }
        }

        [XmlAttribute]
        public bool WantAuthnRequestsSigned {
            get {
                return wantAuthnRequestsSignedField;
            }
            set {
                wantAuthnRequestsSignedField = value;
            }
        }

        [XmlIgnore]
        public bool WantAuthnRequestsSignedSpecified {
            get {
                return wantAuthnRequestsSignedFieldSpecified;
            }
            set {
                wantAuthnRequestsSignedFieldSpecified = value;
            }
        }
    }
}