using System.Xml.Serialization;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [XmlRoot("RequestedAttributeProfiles", Namespace = "https://data.gov.dk/eid/saml/extensions")]
    public class RequestedAttributeProfiles
    {
        [XmlElement(ElementName = "Profile")]
        public string[] Profiles { get; set; }
    }
}