using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public abstract class BaseIDAbstract
    {
        public const string ELEMENT_NAME = "BaseID";

        private string nameQualifierField;

        private string sPNameQualifierField;

        [XmlAttribute]
        public string NameQualifier
        {
            get { return nameQualifierField; }
            set { nameQualifierField = value; }
        }

        [XmlAttribute]
        public string SPNameQualifier
        {
            get { return sPNameQualifierField; }
            set { sPNameQualifierField = value; }
        }
    }
}