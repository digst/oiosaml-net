using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class LogoutRequest : RequestAbstract
    {
        public const string REASON_USER = "urn:oasis:names:tc:SAML:2.0:logout:user";

        public const string REASON_ADMIN = "urn:oasis:names:tc:SAML:2.0:logout:admin";

        public const string ELEMENT_NAME = "LogoutRequest";

        private object itemField;

        private DateTime? notOnOrAfterField;
                
        private string reasonField;
        private string[] sessionIndexField;

        [XmlElement("BaseID", typeof (BaseIDAbstract), Namespace=Saml20Constants.ASSERTION)]
        [XmlElement("EncryptedID", typeof (EncryptedElement), Namespace=Saml20Constants.ASSERTION)]
        [XmlElement("NameID", typeof (NameID), Namespace=Saml20Constants.ASSERTION)]
        public object Item
        {
            get { return itemField; }
            set { itemField = value; }
        }

        [XmlElement("SessionIndex")]
        public string[] SessionIndex
        {
            get { return sessionIndexField; }
            set { sessionIndexField = value; }
        }

        [XmlAttribute]
        public string Reason
        {
            get { return reasonField; }
            set { reasonField = value; }
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
                    return Saml20Utils.ToUTCString(notOnOrAfterField.Value);
                else
                {
                    return null;
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    notOnOrAfterField = null;
                else
                    notOnOrAfterField = Saml20Utils.FromUTCString(value);
            }
        }
    }
}