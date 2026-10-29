using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class EntityDescriptor {
        public const string ELEMENT_NAME = "EntityDescriptor";
        
        private Signature signatureField;
        
        private ExtensionsType1 extensionsField;
        
        private object[] itemsField;
        
        private Organization organizationField;
        
        private Contact[] contactPersonField;
        
        private AdditionalMetadataLocation[] additionalMetadataLocationField;
        
        private string entityIDField;
        
        private DateTime? validUntilField;
        
        private string cacheDurationField;
        
        private string idField;
        
        private XmlAttribute[] anyAttrField;

        [XmlElement(Namespace=Saml20Constants.XMLDSIG)]
        public Signature Signature {
            get {
                return signatureField;
            }
            set {
                signatureField = value;
            }
        }

        public ExtensionsType1 Extensions {
            get { return extensionsField; }
            set { extensionsField = value; }
        }

        [XmlElement("AffiliationDescriptor", typeof(AffiliationDescriptor))]
        [XmlElement("AttributeAuthorityDescriptor", typeof(AttributeAuthorityDescriptor))]
        [XmlElement("AuthnAuthorityDescriptor", typeof(AuthnAuthorityDescriptor))]
        [XmlElement("IDPSSODescriptor", typeof(IDPSSODescriptor))]
        [XmlElement("PDPDescriptor", typeof(PDPDescriptor))]
        [XmlElement("RoleDescriptor", typeof(RoleDescriptor))]
        [XmlElement("SPSSODescriptor", typeof(SPSSODescriptor))]
        public object[] Items {
            get { return itemsField; }
            set { itemsField = value; }
        }

        public Organization Organization {
            get { return organizationField; }
            set { organizationField = value; }
        }

        [XmlElement("ContactPerson")]
        public Contact[] ContactPerson {
            get { return contactPersonField; }
            set { contactPersonField = value; }
        }

        [XmlElement("AdditionalMetadataLocation")]
        public AdditionalMetadataLocation[] AdditionalMetadataLocation {
            get {
                return additionalMetadataLocationField;
            }
            set {
                additionalMetadataLocationField = value;
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string entityID {
            get {
                return entityIDField;
            }
            set {
                entityIDField = value;
            }
        }

        [XmlIgnore]
        public DateTime? validUntil {
            get { return validUntilField; }
            set { validUntilField = value; }
        }

        [XmlAttribute("validUntil")]
        public string validUntilString {
            get
            {
                if (validUntilField == null)
                    return null;
                else
                    return validUntilField.Value.ToUniversalTime().ToString("o");
            }
            set
            {
                if (value == null)
                    validUntilField = null;
                else
                    validUntilField = DateTime.Parse(value);
            }
        }
        
        [XmlAttribute(DataType="duration")]
        public string cacheDuration {
            get {
                return cacheDurationField;
            }
            set {
                cacheDurationField = value;
            }
        }

        [XmlAttribute(DataType="ID")]
        public string ID {
            get {
                return idField;
            }
            set {
                idField = value;
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