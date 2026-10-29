using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XENC, IsNullable=false)]
    public class CipherData
    {
        public const string ELEMENT_NAME = "CipherData";

        private object itemField;

        [XmlElement("CipherReference", typeof (CipherReference))]
        [XmlElement("CipherValue", typeof (byte[]), DataType="base64Binary")]
        public object Item
        {
            get { return itemField; }
            set { itemField = value; }
        }
    }
}