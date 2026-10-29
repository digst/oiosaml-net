using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [DebuggerStepThrough]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class AudienceRestriction : ConditionAbstract
    {
        private List<string> audienceField;

        public new const string ELEMENT_NAME = "AudienceRestriction";

        [XmlElement("Audience", DataType="anyURI")]
        public List<string> Audience
        {
            get { return audienceField; }
            set { audienceField = value; }
        }
    }
}