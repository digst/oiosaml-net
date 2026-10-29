using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.XENC, IncludeInSchema=false)]
    public enum ItemsChoiceType3
    {
        DataReference,

        KeyReference,
    }
}