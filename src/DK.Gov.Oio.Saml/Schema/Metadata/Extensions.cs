using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(TypeName="ExtensionsType", Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.METADATA, IsNullable = false)]
    public class ExtensionsType1
    {
        public const string ELEMENT_NAME = "Extensions";
        
        private XmlElement[] anyField;

        [XmlAnyElement]
        public XmlElement[] Any
        {
            get { return anyField; }
            set { anyField = value; }
        }
    }
}