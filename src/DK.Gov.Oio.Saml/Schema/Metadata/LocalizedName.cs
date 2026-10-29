using System;
using System.Xml.Schema;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.METADATA, IsNullable = false)]
    public class LocalizedName
    {
        public LocalizedName() {}

        public LocalizedName(string value, string lang)
        {
            valueField = value;
            langField = lang;
        }

        public const string ELEMENT_NAME = "OrganizationName";

        private string langField;

        private string valueField;

        [XmlAttribute(Form=XmlSchemaForm.Qualified, Namespace="http://www.w3.org/XML/1998/namespace")]
        public string lang
        {
            get { return langField; }
            set { langField = value; }
        }

        [XmlText]
        public string Value
        {
            get { return valueField; }
            set { valueField = value; }
        }
    }
}