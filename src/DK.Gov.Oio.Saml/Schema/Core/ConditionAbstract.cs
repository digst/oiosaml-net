using System;
using System.Diagnostics;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [XmlInclude(typeof (ProxyRestriction))]
    [XmlInclude(typeof (OneTimeUse))]
    [XmlInclude(typeof (AudienceRestriction))]
    [Serializable]
    [DebuggerStepThrough]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public abstract class ConditionAbstract
    {
        public const string ELEMENT_NAME = "Condition";
    }
}