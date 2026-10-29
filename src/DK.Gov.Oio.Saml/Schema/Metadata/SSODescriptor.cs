using System;
using System.Diagnostics;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [XmlInclude(typeof(SPSSODescriptor))]
    [XmlInclude(typeof(IDPSSODescriptor))]
    [Serializable]
    [DebuggerStepThrough]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    public abstract class SSODescriptor : RoleDescriptor {
        private IndexedEndpoint[] artifactResolutionServiceField;
        
        private Endpoint[] singleLogoutServiceField;
        
        private Endpoint[] manageNameIDServiceField;
        
        private string[] nameIDFormatField;

        [XmlElement("ArtifactResolutionService")]
        public IndexedEndpoint[] ArtifactResolutionService {
            get {
                return artifactResolutionServiceField;
            }
            set {
                artifactResolutionServiceField = value;
            }
        }

        [XmlElement("SingleLogoutService")]
        public Endpoint[] SingleLogoutService {
            get {
                return singleLogoutServiceField;
            }
            set {
                singleLogoutServiceField = value;
            }
        }

        [XmlElement("ManageNameIDService")]
        public Endpoint[] ManageNameIDService {
            get {
                return manageNameIDServiceField;
            }
            set {
                manageNameIDServiceField = value;
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