using System;
using System.Diagnostics;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [DebuggerStepThrough]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class SignatureValue {
        public const string ELEMENT_NAME = "SignatureValue";

        private string idField;
        
        private byte[] valueField;

        [XmlAttribute(DataType="ID")]
        public string Id {
            get {
                return idField;
            }
            set {
                idField = value;
            }
        }

        [XmlText(DataType="base64Binary")]
        public byte[] Value {
            get {
                return valueField;
            }
            set {
                valueField = value;
            }
        }
    }
}