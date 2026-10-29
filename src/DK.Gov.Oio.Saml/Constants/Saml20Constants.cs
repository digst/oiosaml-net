namespace DK.Gov.Oio.Saml.Constants
{
    public class Saml20Constants
    {
        public const string AuthenticationScheme = "OIOSamlAssertion";
        
        public const string Version = "2.0";

        public const string ASSERTION = "urn:oasis:names:tc:SAML:2.0:assertion";

        public const string PROTOCOL = "urn:oasis:names:tc:SAML:2.0:protocol";

        public const string METADATA = "urn:oasis:names:tc:SAML:2.0:metadata";

        public static readonly string[] SAML_NAMESPACES = new string[] { ASSERTION, PROTOCOL, METADATA };

        public const string XMLDSIG = "http://www.w3.org/2000/09/xmldsig#";

        public const string XENC = "http://www.w3.org/2001/04/xmlenc#";

        public const string DEFAULTNAMEIDFORMAT = "urn:oasis:names:tc:SAML:1.0:nameid-format:unspecified";

        public const string METADATA_MIMETYPE = "application/samlmetadata+xml";

        public const string DKSAML20_CLAIMTYPE_PREFIX = "dksaml20/";

        public static class NameIdentifierFormats
        {
            public const string Unspecified = "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified";
            public const string Email = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";
            public const string X509SubjectName = "urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName";
            public const string Windows = "urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName";
            public const string Kerberos = "urn:oasis:names:tc:SAML:2.0:nameid-format:kerberos";
            public const string Entity = "urn:oasis:names:tc:SAML:2.0:nameid-format:entity";
            public const string Persistent = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent";
            public const string Transient = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";
        }

        public static class AttributeNameFormats
        {
            public const string Uri = "urn:oasis:names:tc:SAML:2.0:attrname-format:uri";
        }

        public static class ProtocolBindings
        {
            public const string HTTP_Redirect = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect";
            public const string HTTP_Post = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";
            public const string HTTP_Artifact = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Artifact";
            public const string HTTP_SOAP = "urn:oasis:names:tc:SAML:2.0:bindings:SOAP";
        }

        public static class SubjectConfirmationMethods
        {
            public const string HolderOfKey = "urn:oasis:names:tc:SAML:2.0:cm:holder-of-key";
        }

        public static class Reasons
        {
            public const string User = "urn:oasis:names:tc:SAML:2.0:logout:user";
            public const string Admin = "urn:oasis:names:tc:SAML:2.0:logout:admin";
        }

        public static class StatusCodes
        {
            public const string Success = "urn:oasis:names:tc:SAML:2.0:status:Success";
            public const string Requester = "urn:oasis:names:tc:SAML:2.0:status:Requester";
            public const string Responder = "urn:oasis:names:tc:SAML:2.0:status:Responder";
            public const string VersionMismatch = "urn:oasis:names:tc:SAML:2.0:status:VersionMismatch";
            public const string AuthnFailed = "urn:oasis:names:tc:SAML:2.0:status:AuthnFailed";
            public const string InvalidAttrNameOrValue = "urn:oasis:names:tc:SAML:2.0:status:InvalidAttrNameOrValue";
            public const string InvalidNameIdPolicy = "urn:oasis:names:tc:SAML:2.0:status:InvalidNameIDPolicy";
            public const string NoAuthnContext = "urn:oasis:names:tc:SAML:2.0:status:NoAuthnContext";
            public const string NoAvailableIDP = "urn:oasis:names:tc:SAML:2.0:status:NoAvailableIDP";
            public const string NoPassive = "urn:oasis:names:tc:SAML:2.0:status:NoPassive";
            public const string NoSupportedIDP = "urn:oasis:names:tc:SAML:2.0:status:NoSupportedIDP";
            public const string PartialLogout = "urn:oasis:names:tc:SAML:2.0:status:PartialLogout";
            public const string ProxyCountExceeded = "urn:oasis:names:tc:SAML:2.0:status:ProxyCountExceeded";
            public const string RequestDenied = "urn:oasis:names:tc:SAML:2.0:status:RequestDenied";
            public const string RequestUnsupported = "urn:oasis:names:tc:SAML:2.0:status:RequestUnsupported";
            public const string RequestVersionDeprecated = "urn:oasis:names:tc:SAML:2.0:status:RequestVersionDeprecated";
            public const string RequestVersionTooHigh = "urn:oasis:names:tc:SAML:2.0:status:RequestVersionTooHigh";
            public const string RequestVersionTooLow = "urn:oasis:names:tc:SAML:2.0:status:RequestVersionTooLow";
            public const string ResourceNotRecognized = "urn:oasis:names:tc:SAML:2.0:status:ResourceNotRecognized";
            public const string TooManyResponses = "urn:oasis:names:tc:SAML:2.0:status:TooManyResponses";
            public const string UnknownAttrProfile = "urn:oasis:names:tc:SAML:2.0:status:UnknownAttrProfile";
            public const string UnknownPrincipal = "urn:oasis:names:tc:SAML:2.0:status:UnknownPrincipal";
            public const string UnsupportedBinding = "urn:oasis:names:tc:SAML:2.0:status:UnsupportedBinding";
        }

        internal static class CryptographicAlgorithm
        {
            public static readonly string Aes256Cbc = $"{XENC}aes256-cbc";

            public static readonly string RsaOaepMgf1p = $"{XENC}rsa-oaep-mgf1p";
        }
    }
}