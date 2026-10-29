using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [XmlInclude(typeof(AttributeAuthorityDescriptor))]
    [XmlInclude(typeof(PDPDescriptor))]
    [XmlInclude(typeof(AuthnAuthorityDescriptor))]
    [XmlInclude(typeof(SSODescriptor))]
    [XmlInclude(typeof(SPSSODescriptor))]
    [XmlInclude(typeof(IDPSSODescriptor))]    
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public abstract class RoleDescriptor {
        public const string ELEMENT_NAME = "RoleDescriptor";
        
        private Signature signatureField;
        
        private ExtensionsType1 extensionsField;
        
        private KeyDescriptor[] keyDescriptorField;
        
        private Organization organizationField;
        
        private Contact[] contactPersonField;
        
        private string idField;
        
        private DateTime? validUntilField;
        
        private string cacheDurationField;
        
        private string protocolSupportEnumerationField;
        
        private string errorURLField;
        
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
            get {
                return extensionsField;
            }
            set {
                extensionsField = value;
            }
        }

        [XmlElement("KeyDescriptor")]
        public KeyDescriptor[] KeyDescriptor {
            get {
                return keyDescriptorField;
            }
            set {
                keyDescriptorField = value;
            }
        }

        public Organization Organization {
            get {
                return organizationField;
            }
            set {
                organizationField = value;
            }
        }

        [XmlElement("ContactPerson")]
        public Contact[] ContactPerson {
            get {
                return contactPersonField;
            }
            set {
                contactPersonField = value;
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

        [XmlIgnore]
        public DateTime? validUntil {
            get { return validUntilField; }
            set { validUntilField = value; }
        }

        [XmlAttribute("validUntil")]
        public string validUntilString {
            get
            {
                if (validUntilField.HasValue)
                    return Saml20Utils.ToUTCString(validUntilField.Value);
                else
                    return null;
            }
            set 
            {
                if (string.IsNullOrEmpty(value))
                    validUntilField = null;
                else
                    validUntilField = Saml20Utils.FromUTCString(value);
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

        [XmlAttribute(DataType = "anyURI", AttributeName = "protocolSupportEnumeration")]
        public string protocolSupportEnumerationAsString
        {
            get
            {
                return protocolSupportEnumerationField;
            }
            set
            {
                protocolSupportEnumerationField = value;
            }
        }

        [XmlIgnore]
        public string[] protocolSupportEnumeration
        {
            get
            {
                return protocolSupportEnumerationField.Split(' ');
            }
            set
            {
                protocolSupportEnumerationField = String.Join(" ", value);
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string errorURL {
            get {
                return errorURLField;
            }
            set {
                errorURLField = value;
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