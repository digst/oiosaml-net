using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Tests.Builders;

public class SamlResponseBuilder
{
    public const string Saml = Saml20Constants.ASSERTION;
    public const string Samlp = Saml20Constants.PROTOCOL;

    public string Issuer { get; set; } = TestFederation.IdpEntityId;
    public string? ResponseIssuer { get; set; } = TestFederation.IdpEntityId;
    public string? IssuerFormat { get; set; }
    public string Audience { get; set; } = TestFederation.SpEntityId;
    public string Destination { get; set; } = TestFederation.AcsUrl;
    public string Recipient { get; set; } = TestFederation.AcsUrl;
    public string InResponseTo { get; set; } = TestFederation.AuthnRequestId;
    public string? SubjectConfirmationInResponseTo { get; set; }
    public string ResponseId { get; set; } = "_" + Guid.NewGuid().ToString("N");
    public string AssertionId { get; set; } = "_" + Guid.NewGuid().ToString("N");
    public string NameId { get; set; } = "https://data.gov.dk/model/core/eid/person/uuid/alice";
    public string NameIdFormat { get; set; } = Saml20Constants.NameIdentifierFormats.Persistent;
    public DateTime Now { get; set; } = DateTime.UtcNow;
    public DateTime? AuthnInstant { get; set; }
    public string Loa { get; set; } = LoAValues.Substantial;
    public string LoaAttributeName { get; set; } = OioSaml4Constants.AttributeName_NsisLoA;
    public Dictionary<string, string> ExtraAttributes { get; } = [];
    public string AttributeNameFormat { get; set; } = Saml20Constants.AttributeNameFormats.Uri;
    public string StatusCode { get; set; } = Saml20Constants.StatusCodes.Success;
    public string? SubStatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public X509Certificate2 SigningCertificate { get; set; } = TestCertificates.Idp;
    public X509Certificate2 EncryptionCertificate { get; set; } = TestCertificates.Sp;
    public string SignatureMethod { get; set; } = SignedXml.XmlDsigRSASHA256Url;
    public string DigestMethod { get; set; } = SignedXml.XmlDsigSHA256Url;
    public bool SignAssertion { get; set; } = true;
    public bool SignResponse { get; set; }
    public bool Encrypt { get; set; } = true;
    public string KeyTransportAlgorithm { get; set; } = EncryptedXml.XmlEncRSAOAEPUrl;
    public string DeclaredDataAlgorithm { get; set; } = AllowedAlgorithms.Aes256Gcm;
    public string? OaepDigest { get; set; }
    public string? OaepMgf { get; set; }
    public int? SessionKeyBytes { get; set; }
    public bool TamperCipherValue { get; set; }
    public Action<XmlDocument>? BeforeSigningAssertion { get; set; }
    public Func<XmlElement, XmlElement>? AfterSigningAssertion { get; set; }
    public Action<XmlDocument>? BeforeSigningResponse { get; set; }
    public Action<XmlDocument>? AfterSigningResponse { get; set; }
    public string? RawResponseXml { get; set; }

    public string AssertionXml() => $"""
        <saml:Assertion xmlns:saml="{Saml}" ID="{AssertionId}" Version="2.0" IssueInstant="{Now:o}">
          <saml:Issuer{(IssuerFormat == null ? "" : $" Format=\"{IssuerFormat}\"")}>{Issuer}</saml:Issuer>
          <saml:Subject>
            <saml:NameID Format="{NameIdFormat}">{NameId}</saml:NameID>
            <saml:SubjectConfirmation Method="urn:oasis:names:tc:SAML:2.0:cm:bearer">
              <saml:SubjectConfirmationData NotOnOrAfter="{Now.AddMinutes(5):o}" Recipient="{Recipient}" InResponseTo="{SubjectConfirmationInResponseTo ?? InResponseTo}"/>
            </saml:SubjectConfirmation>
          </saml:Subject>
          <saml:Conditions NotBefore="{Now.AddMinutes(-1):o}" NotOnOrAfter="{Now.AddMinutes(5):o}">
            <saml:AudienceRestriction><saml:Audience>{Audience}</saml:Audience></saml:AudienceRestriction>
          </saml:Conditions>
          <saml:AuthnStatement AuthnInstant="{AuthnInstant ?? Now:o}" SessionIndex="session-1">
            <saml:AuthnContext><saml:AuthnContextClassRef>urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport</saml:AuthnContextClassRef></saml:AuthnContext>
          </saml:AuthnStatement>
          <saml:AttributeStatement>
            <saml:Attribute Name="{LoaAttributeName}" NameFormat="{AttributeNameFormat}"><saml:AttributeValue>{Loa}</saml:AttributeValue></saml:Attribute>
            {string.Concat(ExtraAttributes.Select(a => $"""<saml:Attribute Name="{a.Key}" NameFormat="{AttributeNameFormat}"><saml:AttributeValue>{a.Value}</saml:AttributeValue></saml:Attribute>"""))}
          </saml:AttributeStatement>
        </saml:Assertion>
        """;

    public XmlElement BuildAssertion()
    {
        var doc = SecureXml.Parse(AssertionXml());
        BeforeSigningAssertion?.Invoke(doc);
        if (SignAssertion) XmlMessageSigner.Sign(doc, AssertionId, SigningCertificate, SignatureMethod, DigestMethod);
        var assertion = doc.DocumentElement!;
        return AfterSigningAssertion?.Invoke(assertion) ?? assertion;
    }

    public XmlDocument Build()
    {
        var assertion = BuildAssertion();
        var issuer = ResponseIssuer == null ? "" : $"<saml:Issuer>{ResponseIssuer}</saml:Issuer>";
        var doc = SecureXml.Parse($"""
            <samlp:Response xmlns:samlp="{Samlp}" xmlns:saml="{Saml}" ID="{ResponseId}" Version="2.0" IssueInstant="{Now:o}" Destination="{Destination}" InResponseTo="{InResponseTo}">
              {issuer}
              {Status()}
              {(Encrypt ? EncryptAssertion(assertion) : assertion.OuterXml)}
            </samlp:Response>
            """);
        BeforeSigningResponse?.Invoke(doc);
        if (SignResponse) XmlMessageSigner.Sign(doc, ResponseId, SigningCertificate, SignatureMethod, DigestMethod);
        AfterSigningResponse?.Invoke(doc);
        return doc;
    }

    public string BuildBase64() => Convert.ToBase64String(Encoding.UTF8.GetBytes(RawResponseXml ?? Build().OuterXml));

    private string EncryptAssertion(XmlElement assertion)
    {
        var gcm = DeclaredDataAlgorithm.StartsWith(AllowedAlgorithms.Xmlenc11);
        var key = RandomNumberGenerator.GetBytes(SessionKeyBytes ?? DeclaredDataAlgorithm switch
        {
            EncryptedXml.XmlEncAES128Url or AllowedAlgorithms.Aes128Gcm => 16,
            AllowedAlgorithms.Aes192Gcm => 24,
            _ => 32,
        });
        var plaintext = Encoding.UTF8.GetBytes(assertion.OuterXml);
        var cipherValue = gcm ? EncryptGcm(key, plaintext) : EncryptCbc(key, plaintext);
        if (TamperCipherValue) cipherValue[^1] ^= 1;

        using var rsa = EncryptionCertificate.GetRSAPublicKey()!;
        var oaepHash = OaepDigest switch
        {
            SignedXml.XmlDsigSHA256Url => HashAlgorithmName.SHA256,
            SignedXml.XmlDsigSHA512Url => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA1,
        };
        var encryptedKey = KeyTransportAlgorithm == EncryptedXml.XmlEncRSA15Url
            ? rsa.Encrypt(key, RSAEncryptionPadding.Pkcs1)
            : rsa.Encrypt(key, RSAEncryptionPadding.CreateOaep(oaepHash));
        var digest = OaepDigest == null ? "" : $"""<ds:DigestMethod Algorithm="{OaepDigest}"/>""";
        var mgf = OaepMgf == null ? "" : $"""<xenc11:MGF xmlns:xenc11="{AllowedAlgorithms.Xmlenc11}" Algorithm="{OaepMgf}"/>""";
        return $"""
            <saml:EncryptedAssertion xmlns:saml="{Saml}"><xenc:EncryptedData xmlns:xenc="{Saml20Constants.XENC}" xmlns:ds="{Saml20Constants.XMLDSIG}" Type="{EncryptedXml.XmlEncElementUrl}">
            <xenc:EncryptionMethod Algorithm="{DeclaredDataAlgorithm}"/>
            <ds:KeyInfo><xenc:EncryptedKey><xenc:EncryptionMethod Algorithm="{KeyTransportAlgorithm}">{digest}{mgf}</xenc:EncryptionMethod><xenc:CipherData><xenc:CipherValue>{Convert.ToBase64String(encryptedKey)}</xenc:CipherValue></xenc:CipherData></xenc:EncryptedKey></ds:KeyInfo>
            <xenc:CipherData><xenc:CipherValue>{Convert.ToBase64String(cipherValue)}</xenc:CipherValue></xenc:CipherData>
            </xenc:EncryptedData></saml:EncryptedAssertion>
            """;
    }

    private static byte[] EncryptCbc(byte[] key, byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = RandomNumberGenerator.GetBytes(16);
        return [.. iv, .. aes.EncryptCbc(plaintext, iv)];
    }

    private static byte[] EncryptGcm(byte[] key, byte[] plaintext)
    {
        using var aes = new AesGcm(key, 16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return [.. nonce, .. ciphertext, .. tag];
    }

    private string Status() =>
        $"<samlp:Status><samlp:StatusCode Value=\"{StatusCode}\">{(SubStatusCode == null ? "" : $"<samlp:StatusCode Value=\"{SubStatusCode}\"/>")}</samlp:StatusCode>"
        + (StatusMessage == null ? "" : $"<samlp:StatusMessage>{StatusMessage}</samlp:StatusMessage>") + "</samlp:Status>";
}
