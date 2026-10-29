using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class RetrievalMethod
    {
        public const string ELEMENT_NAME = "RetrievalMethod";

        private Transform[] transformsField;

        private string typeField;
        private string uRIField;

        [XmlArrayItem("Transform", IsNullable=false)]
        public Transform[] Transforms
        {
            get { return transformsField; }
            set { transformsField = value; }
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