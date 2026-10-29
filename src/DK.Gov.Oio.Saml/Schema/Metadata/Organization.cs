using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class Organization {
        public const string ELEMENT_NAME = "Organization";

        private ExtensionsType1 extensionsField;
        
        private LocalizedName[] organizationNameField;
        
        private LocalizedName[] organizationDisplayNameField;
        
        private LocalizedURI[] organizationURLField;
        
        private XmlAttribute[] anyAttrField;

        public ExtensionsType1 Extensions {
            get {
                return extensionsField;
            }
            set {
                extensionsField = value;
            }
        }

        [XmlElement("OrganizationName")]
        public LocalizedName[] OrganizationName {
            get {
                return organizationNameField;
            }
            set {
                organizationNameField = value;
            }
        }

        [XmlElement("OrganizationDisplayName")]
        public LocalizedName[] OrganizationDisplayName {
            get {
                return organizationDisplayNameField;
            }
            set {
                organizationDisplayNameField = value;
            }
        }

        [XmlElement("OrganizationURL")]
        public LocalizedURI[] OrganizationURL {
            get {
                return organizationURLField;
            }
            set {
                organizationURLField = value;
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