using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class Status
    {
        public const string ELEMENT_NAME = "Status";

        private StatusCode statusCodeField;

        private StatusDetail statusDetailField;
        private string statusMessageField;

        public StatusCode StatusCode
        {
            get { return statusCodeField; }
            set { statusCodeField = value; }
        }

        public string StatusMessage
        {
            get { return statusMessageField; }
            set { statusMessageField = value; }
        }

        public StatusDetail StatusDetail
        {
            get { return statusDetailField; }
            set { statusDetailField = value; }
        }
    }
}