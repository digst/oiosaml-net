using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class SignedInfo {
        public const string ELEMENT_NAME = "SignedInfo";
        
        private CanonicalizationMethod canonicalizationMethodField;
        
        private SignatureMethod signatureMethodField;
        
        private Reference[] referenceField;
        
        private string idField;

        public CanonicalizationMethod CanonicalizationMethod {
            get {
                return canonicalizationMethodField;
            }
            set {
                canonicalizationMethodField = value;
            }
        }

        public SignatureMethod SignatureMethod {
            get {
                return signatureMethodField;
            }
            set {
                signatureMethodField = value;
            }
        }

        [XmlElement("Reference")]
        public Reference[] Reference {
            get {
                return referenceField;
            }
            set {
                referenceField = value;
            }
        }

        [XmlAttribute(DataType="ID")]
        public string Id {
            get {
                return idField;
            }
            set {
                idField = value;
            }
        }
    }
}