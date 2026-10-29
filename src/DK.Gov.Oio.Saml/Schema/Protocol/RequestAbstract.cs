using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.XmlDSig;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [XmlInclude(typeof (NameIDMappingRequest))]
    [XmlInclude(typeof (LogoutRequest))]
    [XmlInclude(typeof (ManageNameIDRequest))]
    [XmlInclude(typeof (ArtifactResolve))]
    [XmlInclude(typeof (AuthnRequest))]
    [XmlInclude(typeof (SubjectQueryAbstract))]
    [XmlInclude(typeof (AuthzDecisionQuery))]
    [XmlInclude(typeof (AttributeQuery))]
    [XmlInclude(typeof (AuthnQuery))]
    [XmlInclude(typeof (AssertionIDRequest))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    public abstract class RequestAbstract
    {
        private string consentField;
        private string destinationField;
        private Extensions extensionsField;

        private string idField;

        private DateTime? issueInstantField;
        private NameID issuerField;

        private Signature signatureField;
        private string versionField;

        [XmlElement(Namespace=Saml20Constants.ASSERTION)]
        public NameID Issuer
        {
            get { return issuerField; }
            set { issuerField = value; }
        }

        [XmlElement(Namespace="http://www.w3.org/2000/09/xmldsig#")]
        public Signature Signature
        {
            get { return signatureField; }
            set { signatureField = value; }
        }

        [XmlElement]
        public Extensions Extensions
        {
            get { return extensionsField; }
            set { extensionsField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string ID
        {
            get { return idField; }
            set { idField = value; }
        }

        [XmlAttribute]
        public string Version
        {
            get { return versionField; }
            set { versionField = value; }
        }

        [XmlIgnore]
        public DateTime? IssueInstant
        {
            get { return issueInstantField; }
            set { issueInstantField = value; }
        }

        [XmlAttribute("IssueInstant")]
        public string IssueInstantString
        {
            get 
            {
                if (issueInstantField.HasValue)
                    return Saml20Utils.ToUTCString(issueInstantField.Value);
                else
                {
                    return null;
                }
            }
            set 
            {
                if (string.IsNullOrEmpty(value))
                    issueInstantField = null;
                else
                    issueInstantField = Saml20Utils.FromUTCString(value);
            }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Destination
        {
            get { return destinationField; }
            set { destinationField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Consent
        {
            get { return consentField; }
            set { consentField = value; }
        }
    }
}