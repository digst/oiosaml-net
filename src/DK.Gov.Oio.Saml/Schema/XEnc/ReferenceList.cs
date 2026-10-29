using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.XEnc
{
    [Serializable]
    [XmlType(AnonymousType=true, Namespace=Saml20Constants.XENC)]
    [XmlRoot(Namespace=Saml20Constants.XENC, IsNullable=false)]
    public class ReferenceList
    {
        private ItemsChoiceType3[] itemsElementNameField;
        private ReferenceType1[] itemsField;

        [XmlElement("DataReference", typeof (ReferenceType1))]
        [XmlElement("KeyReference", typeof (ReferenceType1))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public ReferenceType1[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore]
        public ItemsChoiceType3[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }
    }
}