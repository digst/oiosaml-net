using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class Manifest
    {
        public const string ELEMENT_NAME = "Manifest";

        private string idField;
        private Reference[] referenceField;

        [XmlElement("Reference")]
        public Reference[] Reference
        {
            get { return referenceField; }
            set { referenceField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }
    }
}