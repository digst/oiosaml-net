using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [XmlInclude(typeof (AttributeStatement))]
    [XmlInclude(typeof (AuthzDecisionStatement))]
    [XmlInclude(typeof (AuthnStatement))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public abstract class StatementAbstract
    {        
        public const string ELEMENT_NAME = "Statement";
    }
}