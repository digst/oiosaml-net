using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class AssertionIDRequest : RequestAbstract
    {
        public const string ELEMENT_NAME = "AssertionIDRequest";

        private string[] assertionIDRefField;

        [XmlElement("AssertionIDRef", Namespace=Saml20Constants.ASSERTION, DataType="NCName")]
        public string[] AssertionIDRef
        {
            get { return assertionIDRefField; }
            set { assertionIDRefField = value; }
        }
    }
}