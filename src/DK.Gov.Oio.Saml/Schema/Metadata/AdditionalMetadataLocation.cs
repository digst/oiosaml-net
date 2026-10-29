using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class AdditionalMetadataLocation
    {
        public const string ELEMENT_NAME = "AdditionalMetadataLocation";

        private string namespaceField;

        private string valueField;

        [XmlAttribute(DataType="anyURI")]
        public string @namespace
        {
            get { return namespaceField; }
            set { namespaceField = value; }
        }

        [XmlText(DataType="anyURI")]
        public string Value
        {
            get { return valueField; }
            set { valueField = value; }
        }
    }
}