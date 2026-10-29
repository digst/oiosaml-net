using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace = Saml20Constants.METADATA)]
    [XmlRoot(RequestedAttribute.ELEMENT_NAME, Namespace = Saml20Constants.METADATA, IsNullable = false)]
    public class RequestedAttribute : SamlAttribute
    {
        public new const string ELEMENT_NAME = "RequestedAttribute";

        private bool? isRequiredField;

        [XmlIgnore]
        public bool? isRequired
        {
            get { return isRequiredField; }
            set { isRequiredField = value; }
        }

        [XmlAttribute("isRequired")]
        public string isRequiredString
        {
            get {
                if (isRequiredField.HasValue)
                    return XmlConvert.ToString(isRequiredField.Value);
                else
                    return null;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    isRequiredField = null;
                else
                    isRequiredField = XmlConvert.ToBoolean(value);
            }
        }
    }
}