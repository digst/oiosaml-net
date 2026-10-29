using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using Action = DK.Gov.Oio.Saml.Schema.Core.Action;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class AuthzDecisionQuery : SubjectQueryAbstract
    {
        public new const string ELEMENT_NAME = "AuthzDecisionQuery";

        private Action[] actionField;

        private Evidence evidenceField;

        private string resourceField;

        [XmlElement("Action", Namespace=Saml20Constants.ASSERTION)]
        public Action[] Action
        {
            get { return actionField; }
            set { actionField = value; }
        }

        [XmlElement(Namespace=Saml20Constants.ASSERTION)]
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
    }
}