using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [GeneratedCode("xsd", "2.0.50727.42")]
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class IDPList
    {
        public const string ELEMENT_NAME = "IDPList";

        private string getCompleteField;
        private IDPEntry[] iDPEntryField;

        [XmlElement("IDPEntry")]
        public IDPEntry[] IDPEntry
        {
            get { return iDPEntryField; }
            set { iDPEntryField = value; }
        }

        [XmlElement(DataType="anyURI")]
        public string GetComplete
        {
            get { return getCompleteField; }
            set { getCompleteField = value; }
        }
    }
}