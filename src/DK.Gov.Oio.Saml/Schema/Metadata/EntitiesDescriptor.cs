using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class EntitiesDescriptor {
        public const string ELEMENT_NAME = "EntitiesDescriptor";
        
        private Signature signatureField;
        
        private ExtensionsType1 extensionsField;
        
        private object[] itemsField;
        
        private DateTime validUntilField;
        
        private bool validUntilFieldSpecified;
        
        private string cacheDurationField;
        
        private string idField;
        
        private string nameField;

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

        [XmlElement("EntitiesDescriptor", typeof(EntitiesDescriptor))]
        [XmlElement("EntityDescriptor", typeof(EntityDescriptor))]
        public object[] Items {
            get {
                return itemsField;
            }
            set {
                itemsField = value;
            }
        }

        [XmlIgnore]
        public DateTime validUntil {
            get {
                return validUntilField;
            }
            set {
                validUntilField = value;
            }
        }

        [XmlAttribute("validUntil")]
        public string validUntilString {
            get {
                return validUntilField.ToUniversalTime().ToString("o");
            }
            set {
                validUntilField = DateTime.Parse(value);
            }
        }

        [XmlIgnore]
        public bool validUntilSpecified {
            get {
                return validUntilFieldSpecified;
            }
            set {
                validUntilFieldSpecified = value;
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

        [XmlAttribute]
        public string Name {
            get {
                return nameField;
            }
            set {
                nameField = value;
            }
        }
    }
}