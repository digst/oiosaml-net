using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    public enum AuthnContextComparisonType
    {
        exact,

        minimum,

        maximum,

        better,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL, IncludeInSchema=false)]
    public enum ItemsChoiceType7
    {
        [XmlEnum("urn:oasis:names:tc:SAML:2.0:assertion:AuthnContextClassRef")] AuthnContextClassRef,

        [XmlEnum("urn:oasis:names:tc:SAML:2.0:assertion:AuthnContextDeclRef")] AuthnContextDeclRef,
    }
}