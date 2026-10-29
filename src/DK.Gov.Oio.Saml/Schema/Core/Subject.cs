using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class Subject
    {
        public const string ELEMENT_NAME = "Subject";

        private object[] itemsField;

        [XmlElement("BaseID", typeof (BaseIDAbstract))]
        [XmlElement("EncryptedID", typeof (EncryptedElement))]
        [XmlElement("NameID", typeof (NameID))]
        [XmlElement("SubjectConfirmation", typeof (SubjectConfirmation))]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }
    }
}