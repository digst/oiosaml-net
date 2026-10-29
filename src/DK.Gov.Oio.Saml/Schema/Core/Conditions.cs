using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class Conditions
    {
        public const string ELEMENT_NAME = "Conditions";

        private List<ConditionAbstract> itemsField;

        private DateTime? notBeforeField;

        private DateTime? notOnOrAfterField;

        [XmlElement("AudienceRestriction", typeof (AudienceRestriction))]
        [XmlElement("Condition", typeof (ConditionAbstract))]
        [XmlElement("OneTimeUse", typeof (OneTimeUse))]
        [XmlElement("ProxyRestriction", typeof (ProxyRestriction))]
        public List<ConditionAbstract> Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlIgnore]
        public DateTime? NotBefore
        {
            get { return notBeforeField; }
            set { notBeforeField = value; }
        }

        [XmlAttribute("NotBefore")]
        public string NotBeforeString
        {
            get
            {
                if (notBeforeField.HasValue)
                {
                    return Saml20Utils.ToUTCString(notBeforeField.Value);
                }
                else
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

        [XmlIgnore]
        public DateTime? NotOnOrAfter
        {
            get { return notOnOrAfterField; }
            set { notOnOrAfterField = value; }
        }

        [XmlAttribute("NotOnOrAfter")]
        public string NotOnOrAfterString
        {
            get
            {
                if (notOnOrAfterField.HasValue)
                {
                    return Saml20Utils.ToUTCString(notOnOrAfterField.Value);
                }else
                {
                    return null;
                }
            }
            set {
                if(string.IsNullOrEmpty(value))
                    notOnOrAfterField = null;
                else
                    notOnOrAfterField = Saml20Utils.FromUTCString(value);
            }
        }
    }
}