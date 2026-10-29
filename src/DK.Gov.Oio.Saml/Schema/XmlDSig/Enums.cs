using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XmlDSig
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG, IncludeInSchema=false)]
    public enum ItemsChoiceType1
    {
        [XmlEnum("##any:")] Item,

        PGPKeyID,

        PGPKeyPacket,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG, IncludeInSchema=false)]
    public enum ItemsChoiceType
    {
        [XmlEnum("##any:")] Item,

        X509CRL,

        X509Certificate,

        X509IssuerSerial,

        X509SKI,

        X509SubjectName,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.XMLDSIG, IncludeInSchema=false)]
    public enum ItemsChoiceType2
    {
        [XmlEnum("##any:")] Item,

        KeyName,

        KeyValue,

        MgmtData,

        PGPData,

        RetrievalMethod,

        SPKIData,

        X509Data,
    }
}