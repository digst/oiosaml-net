using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class Response : StatusResponse
    {
        public new const string ELEMENT_NAME = "Response";

        private object[] itemsField;

        [XmlElement("Assertion", typeof (Assertion), Namespace=Saml20Constants.ASSERTION)]
        [XmlElement("EncryptedAssertion", typeof (EncryptedElement),
            Namespace=Saml20Constants.ASSERTION)]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }
    }
}