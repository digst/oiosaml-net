using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class AffiliationDescriptor
    {
        public const string ELEMENT_NAME = "AffiliationDescriptor";

        private string[] affiliateMemberField;

        private string affiliationOwnerIDField;
        private XmlAttribute[] anyAttrField;

        private string cacheDurationField;
        private ExtensionsType1 extensionsField;

        private string idField;
        private KeyDescriptor[] keyDescriptorField;
        private Signature signatureField;
        private DateTime? validUntilField;

        [XmlElement(Namespace=Saml20Constants.XMLDSIG)]
        public Signature Signature
        {
            get { return signatureField; }
            set { signatureField = value; }
        }

        public ExtensionsType1 Extensions
        {
            get { return extensionsField; }
            set { extensionsField = value; }
        }

        [XmlElement("AffiliateMember", DataType="anyURI")]
        public string[] AffiliateMember
        {
            get { return affiliateMemberField; }
            set { affiliateMemberField = value; }
        }

        [XmlElement("KeyDescriptor")]
        public KeyDescriptor[] KeyDescriptor
        {
            get { return keyDescriptorField; }
            set { keyDescriptorField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string affiliationOwnerID
        {
            get { return affiliationOwnerIDField; }
            set { affiliationOwnerIDField = value; }
        }

        [XmlIgnore]
        public DateTime? validUntil
        {
            get { return validUntilField; }
            set { validUntilField = value; }
        }

        [XmlAttribute("validUntil")]
        public string validUntilString
        {
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
        public string cacheDuration
        {
            get { return cacheDurationField; }
            set { cacheDurationField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string ID
        {
            get { return idField; }
            set { idField = value; }
        }

        [XmlAnyAttribute]
        public XmlAttribute[] AnyAttr
        {
            get { return anyAttrField; }
            set { anyAttrField = value; }
        }
    }
}