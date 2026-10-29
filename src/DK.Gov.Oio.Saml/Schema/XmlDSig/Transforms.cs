using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class Transforms
    {
        public const string ELEMENT_NAME = "Transforms";

        private Transform[] transformField;

        [XmlElement("Transform")]
        public Transform[] Transform
        {
            get { return transformField; }
            set { transformField = value; }
        }
    }
}