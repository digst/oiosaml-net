using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class IDPEntry
    {
        public const string ELEMENT_NAME = "IDPEntry";

        private string locField;
        private string nameField;
        private string providerIDField;

        [XmlAttribute(DataType="anyURI")]
        public string ProviderID
        {
            get { return providerIDField; }
            set { providerIDField = value; }
        }

        [XmlAttribute]
        public string Name
        {
            get { return nameField; }
            set { nameField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Loc
        {
            get { return locField; }
            set { locField = value; }
        }
    }
}