using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class NameIDMappingRequest : RequestAbstract
    {
        public const string ELEMENT_NAME = "NameIDMappingRequest";

        private object itemField;

        private NameIDPolicy nameIDPolicyField;

        [XmlElement("BaseID", typeof (BaseIDAbstract), Namespace=Saml20Constants.ASSERTION)]
        [XmlElement("EncryptedID", typeof (EncryptedElement), Namespace=Saml20Constants.ASSERTION)]
        [XmlElement("NameID", typeof (NameID), Namespace=Saml20Constants.ASSERTION)]
        public object Item
        {
            get { return itemField; }
            set { itemField = value; }
        }

        public NameIDPolicy NameIDPolicy
        {
            get { return nameIDPolicyField; }
            set { nameIDPolicyField = value; }
        }
    }
}