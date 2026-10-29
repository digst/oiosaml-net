using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Protocol;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class SubjectConfirmation
    {
        public const string ELEMENT_NAME = "SubjectConfirmation";

        public const string BEARER_METHOD = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

        private object itemField;

        private string methodField;
        private SubjectConfirmationData subjectConfirmationDataField;

        [XmlElement("BaseID", typeof (BaseIDAbstract))]
        [XmlElement("EncryptedID", typeof (EncryptedElement))]
        [XmlElement("NameID", typeof (NameID))]
        public object Item
        {
            get { return itemField; }
            set { itemField = value; }
        }

        public SubjectConfirmationData SubjectConfirmationData
        {
            get { return subjectConfirmationDataField; }
            set { subjectConfirmationDataField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Method
        {
            get { return methodField; }
            set { methodField = value; }
        }
    }
}