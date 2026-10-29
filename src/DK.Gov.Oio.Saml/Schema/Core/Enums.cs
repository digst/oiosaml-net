using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Core
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION, IncludeInSchema=false)]
    public enum ItemsChoiceType6
    {
        Assertion,
        AssertionIDRef,
        AssertionURIRef,
        EncryptedAssertion,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION)]
    public enum DecisionType
    {
        Permit,
        Deny,
        Indeterminate,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION, IncludeInSchema=false)]
    public enum ItemsChoiceType4
    {
        [XmlEnum("##any:")] Item,
        Assertion,
        AssertionIDRef,
        AssertionURIRef,
        EncryptedAssertion,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.ASSERTION, IncludeInSchema=false)]
    public enum ItemsChoiceType5
    {
        AuthnContextClassRef,
        AuthnContextDecl,
        AuthnContextDeclRef,
    }
}