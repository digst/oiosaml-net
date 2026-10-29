using System;
using System.Xml;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [XmlInclude(typeof(KeyInfoConfirmationData))]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace = Saml20Constants.ASSERTION, IsNullable = false)]
    public class SubjectConfirmationData
    {
        public const string ELEMENT_NAME = "SubjectConfirmationData";

        private DateTime? notOnOrAfterField;
        [XmlIgnore] 
        public DateTime? NotOnOrAfter
        {
            get {
                return notOnOrAfterField;
            }
            set { notOnOrAfterField = value; }
        }

        [XmlAttribute("NotOnOrAfter")] 
        public string NotOnOrAfterString
        {
            get 
            { 
                if(notOnOrAfterField.HasValue)
                {
                    return Saml20Utils.ToUTCString(notOnOrAfterField.Value);
                }
                else
                {
                    return null;
                }
            }
            set 
            {
                if(string.IsNullOrEmpty(value))
                    notOnOrAfterField = null;
                else
                    notOnOrAfterField = Saml20Utils.FromUTCString(value);
            }
        }

        private DateTime? notBeforeField;

        [XmlIgnore]
        public DateTime? NotBefore
        {
            get { return notBeforeField; }
            set { notBeforeField = value;}
        }

        [XmlAttribute("NotBefore")]
        public string NotBeforeString
        {
            get
            {
                if(notBeforeField.HasValue)
                {
                    return Saml20Utils.ToUTCString(notBeforeField.Value);
                }else
                {
                    return null;
                }
            }
            set
            {
                if(string.IsNullOrEmpty(value))
                    notBeforeField = null;
                else
                    notBeforeField = Saml20Utils.FromUTCString(value);
            }
        }

        private string recipientField;

        [XmlAttribute("Recipient", DataType = "anyURI")] 
        public string Recipient
        {
            get { return recipientField; }
            set{ recipientField = value;}
        }

        private string inResponseToField;

        [XmlAttribute("InResponseTo", DataType = "NCName")] 
        public string InResponseTo
        {
            get { return inResponseToField; }
            set { inResponseToField = value; }
        }

        private string addressField;

        [XmlAttribute("Address", DataType = "string")] 
        public string Address
        {
            get { return addressField; }
            set { addressField = value; }
        }

        private XmlAttribute[] anyAttrField;

        [XmlAnyAttribute] public XmlAttribute[] AnyAttr
        {
            get { return anyAttrField; }
            set { anyAttrField = value; }
        }

        private XmlElement[] anyElementField;

        [XmlAnyElement]
        public XmlElement[] AnyElements
        {
            get { return anyElementField; }
            set { anyElementField = value; }
        }
    }
}