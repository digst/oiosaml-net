using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class Reference
    {
        public const string ELEMENT_NAME = "Reference";

        private DigestMethod digestMethodField;

        private byte[] digestValueField;

        private string idField;
        private Transform[] transformsField;

        private string typeField;
        private string uRIField;

        [XmlArrayItem("Transform", IsNullable=false)]
        public Transform[] Transforms
        {
            get { return transformsField; }
            set { transformsField = value; }
        }

        public DigestMethod DigestMethod
        {
            get { return digestMethodField; }
            set { digestMethodField = value; }
        }

        [XmlElement(DataType="base64Binary")]
        public byte[] DigestValue
        {
            get { return digestValueField; }
            set { digestValueField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string URI
        {
            get { return uRIField; }
            set { uRIField = value; }
        }

        [XmlAttribute(DataType="anyURI")]
        public string Type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }
}