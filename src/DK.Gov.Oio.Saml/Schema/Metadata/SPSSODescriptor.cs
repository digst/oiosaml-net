using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class SPSSODescriptor : SSODescriptor {
        public new const string ELEMENT_NAME = "SPSSODescriptor";
        
        private IndexedEndpoint[] assertionConsumerServiceField;
        
        private AttributeConsumingService[] attributeConsumingServiceField;
        
        private bool? authnRequestsSignedField;
        
        private bool? wantAssertionsSignedField;

        [XmlElement("AssertionConsumerService")]
        public IndexedEndpoint[] AssertionConsumerService {
            get {
                return assertionConsumerServiceField;
            }
            set {
                assertionConsumerServiceField = value;
            }
        }

        [XmlElement("AttributeConsumingService")]
        public AttributeConsumingService[] AttributeConsumingService {
            get {
                return attributeConsumingServiceField;
            }
            set {
                attributeConsumingServiceField = value;
            }
        }

        [XmlAttribute]
        public string AuthnRequestsSigned {
            get 
            {
                if (authnRequestsSignedField == null)
                    return null;
                else
                    return XmlConvert.ToString(authnRequestsSignedField.Value);
            }
            set 
            {
                if (string.IsNullOrEmpty(value))
                    authnRequestsSignedField = null;
                else 
                    authnRequestsSignedField = XmlConvert.ToBoolean(value);
            }
        }

        [XmlAttribute]
        public string WantAssertionsSigned {
            get 
            {
                if (wantAssertionsSignedField == null)
                    return null;
                else
                    return XmlConvert.ToString(wantAssertionsSignedField.Value);
            }
            set 
            {
                if (string.IsNullOrEmpty(value))
                    wantAssertionsSignedField = null;
                else 
                    wantAssertionsSignedField = XmlConvert.ToBoolean(value);
            }
        }
    }
}