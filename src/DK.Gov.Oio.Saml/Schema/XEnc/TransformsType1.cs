using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.XmlDSig;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(TypeName="TransformsType", Namespace=Saml20Constants.XENC)]
    public class TransformsType1
    {
        public const string ELEMENT_NAME = "TransformsType";

        private Transform[] transformField;

        [XmlElement("Transform", Namespace=Saml20Constants.XMLDSIG)]
        public Transform[] Transform
        {
            get { return transformField; }
            set { transformField = value; }
        }
    }
}