using System.Xml.Serialization;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    public enum AppSwitchPlatform
    {
        [XmlEnum(Name="iOS")]
        iOS,
        
        [XmlEnum(Name="Android")]
        Android
    }
}