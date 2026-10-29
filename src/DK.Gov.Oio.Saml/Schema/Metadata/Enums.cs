using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Metadata
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    public enum ContactType {
        technical,

        support,

        administrative,

        billing,

        other,
    }

    [Serializable]
    [XmlType(Namespace=Saml20Constants.METADATA)]
    public enum KeyTypes
    {
        encryption,
        signing,
    }
}