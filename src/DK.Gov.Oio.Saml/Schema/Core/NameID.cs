using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class NameID
    {
        public const string ELEMENT_NAME = "NameID";

        private string formatField;
        private string nameQualifierField;

        private string sPNameQualifierField;

        private string sPProvidedIDField;

        private string valueField;

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

        [XmlAttribute(DataType="anyURI")]
        public string Format
        {
            get { return formatField; }
            set { formatField = value; }
        }

        [XmlAttribute]
        public string SPProvidedID
        {
            get { return sPProvidedIDField; }
            set { sPProvidedIDField = value; }
        }

        [XmlText]
        public string Value
        {
            get { return valueField; }
            set { valueField = value; }
        }
    }
}