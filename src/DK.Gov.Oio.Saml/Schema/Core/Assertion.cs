using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class Assertion
    {
        public Assertion()
        {
            versionField = Saml20Constants.Version;
        }

        public const string ELEMENT_NAME = "Assertion";

        private Advice adviceField;
        private Conditions conditionsField;
        private string idField;

        private DateTime? issueInstantField;
        private NameID issuerField;
        private StatementAbstract[] itemsField;

        private Signature signatureField;

        private Subject subjectField;

        private string versionField;

        public NameID Issuer
        {
            get { return issuerField; }
            set { issuerField = value; }
        }

        [XmlElement(Namespace=Saml20Constants.XMLDSIG)]
        public Signature Signature
        {
            get { return signatureField; }
            set { signatureField = value; }
        }

        public Subject Subject
        {
            get { return subjectField; }
            set { subjectField = value; }
        }

        public Conditions Conditions
        {
            get { return conditionsField; }
            set { conditionsField = value; }
        }

        public Advice Advice
        {
            get { return adviceField; }
            set { adviceField = value; }
        }

        [XmlElement("AttributeStatement", typeof (AttributeStatement))]
        [XmlElement("AuthnStatement", typeof (AuthnStatement))]
        [XmlElement("AuthzDecisionStatement", typeof (AuthzDecisionStatement))]
        [XmlElement("Statement", typeof (StatementAbstract))]
        public StatementAbstract[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        public List<AttributeStatement> GetAttributeStatements()
        {
            return GetStatements<AttributeStatement>();
        }

        public List<AuthnStatement> GetAuthnStatements()
        {
            return GetStatements<AuthnStatement>();
        }

        private List<T> GetStatements<T>() where T : StatementAbstract
        {            
            List<T> result = new List<T>(1);
            foreach (StatementAbstract statementAbstract in itemsField)
                if (statementAbstract is T)
                    result.Add((T)statementAbstract);

            return result;
        }

        [XmlAttribute]
        public string Version
        {
            get { return versionField; }
            set { versionField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string ID
        {
            get { return idField; }
            set { idField = value; }
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
                {
                    return Saml20Utils.ToUTCString(issueInstantField.Value);
                }else
                {
                    return null;
                }
            }
            set
            {
                if(string.IsNullOrEmpty(value))
                    issueInstantField = null;
                else
                    issueInstantField = Saml20Utils.FromUTCString(value);
            }
        }
    }
}