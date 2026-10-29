using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]    
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class ManageNameIDRequest : RequestAbstract
    {
        public const string ELEMENT_NAME = "ManageNameIDRequest";

        private object item1Field;
        private object itemField;

        [XmlElement("EncryptedID", typeof (EncryptedElement), Namespace=Saml20Constants.ASSERTION)]
        [XmlElement("NameID", typeof (NameID), Namespace=Saml20Constants.ASSERTION)]
        public object Item
        {
            get { return itemField; }
            set { itemField = value; }
        }

        [XmlElement("NewEncryptedID", typeof (EncryptedElement))]
        [XmlElement("NewID", typeof (string))]
        [XmlElement("Terminate", typeof (Terminate))]
        public object Item1
        {
            get { return item1Field; }
            set { item1Field = value; }
        }
    }
}