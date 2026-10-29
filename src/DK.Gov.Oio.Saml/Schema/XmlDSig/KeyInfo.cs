using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]    
    [XmlType(Namespace=Saml20Constants.XMLDSIG)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.XMLDSIG, IsNullable=false)]
    public class KeyInfo
    {
        public const string ELEMENT_NAME = "KeyInfo";

        public static explicit operator System.Security.Cryptography.Xml.KeyInfo(KeyInfo ki)
        {
            System.Security.Cryptography.Xml.KeyInfo result = new System.Security.Cryptography.Xml.KeyInfo();
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.LoadXml(Serialization.SerializeToXmlString(ki));
            result.LoadXml(doc.DocumentElement);
            return result;
        }

        private string idField;
        private ItemsChoiceType2[] itemsElementNameField;
        private object[] itemsField;

        private string[] textField;

        [XmlAnyElement]
        [XmlElement("KeyName", typeof (string))]
        [XmlElement("KeyValue", typeof (KeyValue))]
        [XmlElement("MgmtData", typeof (string))]
        [XmlElement("PGPData", typeof (PGPData))]
        [XmlElement("RetrievalMethod", typeof (RetrievalMethod))]
        [XmlElement("SPKIData", typeof (SPKIData))]
        [XmlElement("X509Data", typeof (X509Data))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType2[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }

        [XmlText]
        public string[] Text
        {
            get { return textField; }
            set { textField = value; }
        }

        [XmlAttribute(DataType="ID")]
        public string Id
        {
            get { return idField; }
            set { idField = value; }
        }
    }
}