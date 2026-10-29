using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class StatusCode
    {
        public const string ELEMENT_NAME = "StatusCode";

        private StatusCode statusCodeField;

        private string valueField;

        [XmlElement("StatusCode", Namespace = Saml20Constants.PROTOCOL)]
        public StatusCode SubStatusCode
        {
            get { return statusCodeField; }
            set { statusCodeField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Value
        {
            get { return valueField; }
            set { valueField = value; }
        }
    }
}