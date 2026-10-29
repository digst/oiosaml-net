using System;
using System.Xml.Schema;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.XmlDSig;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [XmlInclude(typeof (NameIDMappingResponse))]
    [XmlInclude(typeof (ArtifactResponse))]
    [XmlInclude(typeof (LogoutResponse))]
    [XmlInclude(typeof (Response))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.PROTOCOL, IsNullable = false)]
    public class StatusResponse
    {
        public const string ELEMENT_NAME = "ManageNameIDResponse";

        private string consentField;
        private string destinationField;
        private Extensions extensionsField;

        private string idField;

        private string inResponseToField;

        private DateTime? issueInstantField;
        private NameID issuerField;

        private Signature signatureField;
        private Status statusField;
        private string versionField;

        [XmlElement(Namespace=Saml20Constants.ASSERTION, Form = XmlSchemaForm.Qualified)]
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

        public Extensions Extensions
        {
            get { return extensionsField; }
            set { extensionsField = value; }
        }

        public Status Status
        {
            get { return statusField; }
            set { statusField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string ID
        {
            get { return idField; }
            set { idField = value; }
        }

        [XmlAttribute(DataType="NCName")]
        public string InResponseTo
        {
            get { return inResponseToField; }
            set { inResponseToField = value; }
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
                    return null;
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