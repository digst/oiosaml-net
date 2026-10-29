namespace DK.Gov.Oio.Saml.Schema.BasicPrivilegeProfile
{
    [System.Serializable()]
    [System.Xml.Serialization.XmlType(Namespace = Constants.XmlNamespace)]
    public partial class Constraint
    {
        [System.Xml.Serialization.XmlAttribute()]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }
    }
}
