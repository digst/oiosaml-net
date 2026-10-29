using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.METADATA, IsNullable=false)]
    public class AttributeConsumingService
    {
        public const string ELEMENT_NAME = "AttributeConsumingService";

        private ushort indexField;

        private bool? isDefaultField;

        private RequestedAttribute[] requestedAttributeField;
        private LocalizedName[] serviceDescriptionField;
        private LocalizedName[] serviceNameField;

        [XmlElement("ServiceName")]
        public LocalizedName[] ServiceName
        {
            get { return serviceNameField; }
            set { serviceNameField = value; }
        }

        [XmlElement("ServiceDescription")]
        public LocalizedName[] ServiceDescription
        {
            get { return serviceDescriptionField; }
            set { serviceDescriptionField = value; }
        }

        [XmlElement("RequestedAttribute")]
        public RequestedAttribute[] RequestedAttribute
        {
            get { return requestedAttributeField; }
            set { requestedAttributeField = value; }
        }

        [XmlAttribute]
        public ushort index
        {
            get { return indexField; }
            set { indexField = value; }
        }

        [XmlIgnore]
        public bool? isDefault
        {
            get { return isDefaultField; }
            set { isDefaultField = value; }
        } 

        [XmlAttribute("isDefault")]
        public string isDefaultString
        {
            get
            {
                if (isDefaultField == null)
                    return null;
                else
                    return XmlConvert.ToString(isDefaultField.Value);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    isDefaultField = null;
                else 
                    isDefaultField = XmlConvert.ToBoolean(value);
            }
        }
    }
}