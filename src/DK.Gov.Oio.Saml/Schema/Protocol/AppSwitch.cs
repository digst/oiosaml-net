using System;
using System.Xml.Serialization;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [XmlRoot("AppSwitch", Namespace = "https://data.gov.dk/eid/saml/extensions")]
    
    [Serializable]
    public class AppSwitch
    {
        public AppSwitchPlatform Platform { get; set; }

        public string ReturnURL { get; set; }
    }
}