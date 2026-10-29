namespace DK.Gov.Oio.Saml.Schema.BasicPrivilegeProfile
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(Namespace = Constants.XmlNamespace)]
    public partial class PrivilegeGroupType
    {
        [System.Xml.Serialization.XmlElement("Privilege", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string[] Privilege { get; set; }

        [System.Xml.Serialization.XmlElement("Constraint", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public Constraint[] Constraint { get; set; }

        [System.Xml.Serialization.XmlAttribute()]
        public string Scope { get; set; }
    }
}
