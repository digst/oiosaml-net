using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class AuthzDecisionStatement : StatementAbstract
    {
        public new const string ELEMENT_NAME = "AuthzDecisionStatement";

        private Action[] actionField;
        private DecisionType decisionField;

        private Evidence evidenceField;

        private string resourceField;

        [XmlElement("Action")]
        public Action[] Action
        {
            get { return actionField; }
            set { actionField = value; }
        }

        public Evidence Evidence
        {
            get { return evidenceField; }
            set { evidenceField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Resource
        {
            get { return resourceField; }
            set { resourceField = value; }
        }

        [XmlAttribute]
        public DecisionType Decision
        {
            get { return decisionField; }
            set { decisionField = value; }
        }
    }
}