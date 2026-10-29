using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.ASSERTION, IsNullable=false)]
    public class AuthnStatement : StatementAbstract
    {
        public new const string ELEMENT_NAME = "AuthnStatement";

        private AuthnContext authnContextField;

        private DateTime? authnInstantField;

        private string sessionIndexField;

        private DateTime? sessionNotOnOrAfterField;

        private SubjectLocality subjectLocalityField;

        public SubjectLocality SubjectLocality
        {
            get { return subjectLocalityField; }
            set { subjectLocalityField = value; }
        }

        public AuthnContext AuthnContext
        {
            get { return authnContextField; }
            set { authnContextField = value; }
        }

        [XmlIgnore]
        public DateTime? AuthnInstant
        {
            get { return authnInstantField; }
            set { authnInstantField = value; }
        }

        [XmlAttribute("AuthnInstant")]
        public string AuthnInstantString
        {
            get { 
                if(authnInstantField.HasValue)
                {
                    return Saml20Utils.ToUTCString(authnInstantField.Value);
                }else
                {
                    return null;
                }
            }
            set { authnInstantField = Saml20Utils.FromUTCString(value); }
        }

        [XmlAttribute]
        public string SessionIndex
        {
            get { return sessionIndexField; }
            set { sessionIndexField = value; }
        }

        [XmlIgnore]
        public DateTime? SessionNotOnOrAfter
        {
            get { return sessionNotOnOrAfterField; }
            set { sessionNotOnOrAfterField = value; }
        }

        [XmlAttribute("SessionNotOnOrAfter")]
        public string SessionNotOnOrAfterString
        {
            get
            {
                if(sessionNotOnOrAfterField.HasValue)
                {
                    return Saml20Utils.ToUTCString(sessionNotOnOrAfterField.Value);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if(string.IsNullOrEmpty(value))
                    sessionNotOnOrAfterField = null;
                else
                    sessionNotOnOrAfterField = Saml20Utils.FromUTCString(value);
            }
        }
    }
}