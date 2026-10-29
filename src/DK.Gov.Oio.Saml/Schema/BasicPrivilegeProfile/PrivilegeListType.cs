namespace DK.Gov.Oio.Saml.Schema.BasicPrivilegeProfile
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(Namespace = Constants.XmlNamespace)]
    [System.Xml.Serialization.XmlRoot("PrivilegeList", Namespace = Constants.XmlNamespace, IsNullable = false)]
    public partial class PrivilegeListType
    {
        [System.Xml.Serialization.XmlElement("PrivilegeGroup", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public PrivilegeGroupType[] PrivilegeGroups { get; set; }
    }
}
