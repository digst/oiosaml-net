using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [XmlInclude(typeof (AuthzDecisionQuery))]
    [XmlInclude(typeof (AttributeQuery))]
    [XmlInclude(typeof (AuthnQuery))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.PROTOCOL, IsNullable = false)]
    public abstract class SubjectQueryAbstract : RequestAbstract
    {
        public const string ELEMENT_NAME = "SubjectQuery";

        private Subject subjectField;
        
        [XmlElement(Namespace=Saml20Constants.ASSERTION)]
        public Subject Subject
        {
            get { return subjectField; }
            set { subjectField = value; }
        }
    }
}