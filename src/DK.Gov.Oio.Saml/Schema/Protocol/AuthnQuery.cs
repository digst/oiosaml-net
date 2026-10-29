using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class AuthnQuery : SubjectQueryAbstract
    {
        public new const string ELEMENT_NAME = "AuthnQuery";

        private RequestedAuthnContext requestedAuthnContextField;

        private string sessionIndexField;

        public RequestedAuthnContext RequestedAuthnContext
        {
            get { return requestedAuthnContextField; }
            set { requestedAuthnContextField = value; }
        }

        [XmlAttribute]
        public string SessionIndex
        {
            get { return sessionIndexField; }
            set { sessionIndexField = value; }
        }
    }
}