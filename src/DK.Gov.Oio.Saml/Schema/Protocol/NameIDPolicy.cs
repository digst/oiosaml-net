using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class NameIDPolicy
    {
        public const string ELEMENT_NAME = "NameIDPolicy";

        private bool? allowCreateField;

        private string formatField;

        private string sPNameQualifierField;

        [XmlAttribute(DataType="anyURI")]
        public string Format
        {
            get { return formatField; }
            set { formatField = value; }
        }

        [XmlAttribute]
        public string SPNameQualifier
        {
            get { return sPNameQualifierField; }
            set { sPNameQualifierField = value; }
        }

        [XmlIgnore]
        public bool? AllowCreate
        {
            get { return allowCreateField; }
            set { allowCreateField = value; }
        }

        [XmlAttribute(AttributeName = "AllowCreate")]
        public string AllowCreateString
        {
            get
            {
                if (allowCreateField.HasValue)
                    return XmlConvert.ToString(allowCreateField.Value);
                else
                    return null;
            }
            set {
                if (string.IsNullOrEmpty(value))
                    allowCreateField = null;
                else
                    allowCreateField = XmlConvert.ToBoolean(value);
            }
        }
    }
}