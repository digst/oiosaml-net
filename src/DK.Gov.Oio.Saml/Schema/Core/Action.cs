using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class Action
    {
        public const string ELEMENT_NAME = "Action";

        private string namespaceField;

        private string valueField;

        [XmlAttribute(DataType="anyURI")]
        public string Namespace
        {
            get { return namespaceField; }
            set { namespaceField = value; }
        }

        [XmlText]
        public string Value
        {
            get { return valueField; }
            set { valueField = value; }
        }
    }
}