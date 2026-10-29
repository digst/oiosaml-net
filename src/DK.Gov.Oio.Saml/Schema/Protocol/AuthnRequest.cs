using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class AuthnRequest : RequestAbstract
    {
        public const string ELEMENT_NAME = "AuthnRequest";

        private ushort assertionConsumerServiceIndexField;

        private bool assertionConsumerServiceIndexFieldSpecified;

        private string assertionConsumerServiceURLField;

        private ushort attributeConsumingServiceIndexField;

        private bool attributeConsumingServiceIndexFieldSpecified;
        private Conditions conditionsField;

        private bool? forceAuthnField;

        private bool? isPassiveField;

        private NameIDPolicy nameIDPolicyField;

        private string protocolBindingField;

        private string providerNameField;
        private RequestedAuthnContext requestedAuthnContextField;

        private Scoping scopingField;
        private Subject subjectField;

        [XmlElement(Namespace=Saml20Constants.ASSERTION)]
        public Subject Subject
        {
            get { return subjectField; }
            set { subjectField = value; }
        }

        public NameIDPolicy NameIDPolicy
        {
            get { return nameIDPolicyField; }
            set { nameIDPolicyField = value; }
        }

        [XmlElement(Namespace=Saml20Constants.ASSERTION)]
        public Conditions Conditions
        {
            get { return conditionsField; }
            set { conditionsField = value; }
        }

        public RequestedAuthnContext RequestedAuthnContext
        {
            get { return requestedAuthnContextField; }
            set { requestedAuthnContextField = value; }
        }

        public Scoping Scoping
        {
            get { return scopingField; }
            set { scopingField = value; }
        }

        [XmlIgnore]
        public bool? ForceAuthn
        {
            get { return forceAuthnField; }
            set { forceAuthnField = value; }
        }

        [XmlAttribute(AttributeName = "ForceAuthn")]
        public string ForceAuthnString
        {
            get
            {
                if (forceAuthnField.HasValue)
                    return forceAuthnField.Value.ToString().ToLower();
                return null;
            }
            set
            {
                bool val;
                if (bool.TryParse(value, out val))
                {
                    forceAuthnField = val;
                }
            }
        }

        [XmlIgnore]
        public bool? IsPassive
        {
            get { return isPassiveField; }
            set { isPassiveField = value; }
        }

        [XmlAttribute(AttributeName="IsPassive")]
        public string IsPassiveString
        {
            get
            {
                if (isPassiveField.HasValue)
                    return isPassiveField.Value.ToString().ToLower();
                return "false";
            }
            set
            {
                bool val;
                if (bool.TryParse(value, out val))
                {
                    isPassiveField = val;
                }
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string ProtocolBinding
        {
            get { return protocolBindingField; }
            set { protocolBindingField = value; }
        }

        [XmlAttribute]
        public ushort AssertionConsumerServiceIndex
        {
            get { return assertionConsumerServiceIndexField; }
            set { assertionConsumerServiceIndexField = value; }
        }

        [XmlIgnore]
        public bool AssertionConsumerServiceIndexSpecified
        {
            get { return assertionConsumerServiceIndexFieldSpecified; }
            set { assertionConsumerServiceIndexFieldSpecified = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string AssertionConsumerServiceURL
        {
            get { return assertionConsumerServiceURLField; }
            set { assertionConsumerServiceURLField = value; }
        }

        [XmlAttribute]
        public ushort AttributeConsumingServiceIndex
        {
            get { return attributeConsumingServiceIndexField; }
            set { attributeConsumingServiceIndexField = value; }
        }

        [XmlIgnore]
        public bool AttributeConsumingServiceIndexSpecified
        {
            get { return attributeConsumingServiceIndexFieldSpecified; }
            set { attributeConsumingServiceIndexFieldSpecified = value; }
        }

        [XmlAttribute]
        public string ProviderName
        {
            get { return providerNameField; }
            set { providerNameField = value; }
        }
    }
}