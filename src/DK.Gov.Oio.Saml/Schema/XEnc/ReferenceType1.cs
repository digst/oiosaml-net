using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(TypeName="ReferenceType", Namespace=Saml20Constants.XENC)]
    public class ReferenceType1
    {
        public const string ELEMENT_NAME = "ReferenceType";

        private XmlElement[] anyField;

        private string uRIField;

        [XmlAnyElement]
        public XmlElement[] Any
        {
            get { return anyField; }
            set { anyField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string URI
        {
            get { return uRIField; }
            set { uRIField = value; }
        }
    }
}