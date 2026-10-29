using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class ProxyRestriction : ConditionAbstract
    {
        public new const string ELEMENT_NAME = "ProxyRestriction";

        private string[] audienceField;

        private string countField;

        [XmlElement("Audience", DataType="anyURI")]
        public string[] Audience
        {
            get { return audienceField; }
            set { audienceField = value; }
        }

        [XmlAttribute(DataType="nonNegativeInteger")]
        public string Count
        {
            get { return countField; }
            set { countField = value; }
        }
    }
}