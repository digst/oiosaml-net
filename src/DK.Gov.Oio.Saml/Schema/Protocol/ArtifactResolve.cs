using System;
using System.Xml.Serialization;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.Schema.Protocol
{
    [Serializable]
    [XmlType(Namespace=Saml20Constants.PROTOCOL)]
    [XmlRoot(ELEMENT_NAME, Namespace=Saml20Constants.PROTOCOL, IsNullable=false)]
    public class ArtifactResolve : RequestAbstract
    {
        public const string ELEMENT_NAME = "ArtifactResolve";

        private string artifactField;

        public string Artifact
        {
            get { return artifactField; }
            set { artifactField = value; }
        }
    }
}