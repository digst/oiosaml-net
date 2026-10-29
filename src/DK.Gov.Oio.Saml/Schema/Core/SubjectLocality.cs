using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class SubjectLocality
    {
        public const string ELEMENT_NAME = "SubjectLocality";

        private string addressField;

        private string dNSNameField;

        [XmlAttribute]
        public string Address
        {
            get { return addressField; }
            set { addressField = value; }
        }

        [XmlAttribute]
        public string DNSName
        {
            get { return dNSNameField; }
            set { dNSNameField = value; }
        }
    }
}