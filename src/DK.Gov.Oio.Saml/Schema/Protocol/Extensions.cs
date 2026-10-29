using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class Extensions
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