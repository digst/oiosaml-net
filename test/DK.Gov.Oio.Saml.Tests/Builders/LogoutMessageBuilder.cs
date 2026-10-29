using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Web;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Tests.Builders;

public class LogoutMessageBuilder
{
    public string Id { get; set; } = "_" + Guid.NewGuid().ToString("N");
    public DateTime IssueInstant { get; set; } = DateTime.UtcNow;
    public string Issuer { get; set; } = TestFederation.IdpEntityId;
    public string Destination { get; set; } = TestFederation.SloUrl;
    public string NameId { get; set; } = "https://data.gov.dk/model/core/eid/person/uuid/alice";
    public string NameIdFormat { get; set; } = Saml20Constants.NameIdentifierFormats.Persistent;
    public string? InResponseTo { get; set; }
    public string StatusCode { get; set; } = Saml20Constants.StatusCodes.Success;
    public string? SubStatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public bool Sign { get; set; } = true;
    public X509Certificate2 SigningCertificate { get; set; } = TestCertificates.Idp;
    public string SigAlg { get; set; } = SignedXml.XmlDsigRSASHA256Url;
    public Action<XmlDocument>? BeforeSigning { get; set; }
    public string? RelayState { get; set; }
    // How the IdP URL-encodes the redirect query; HttpUtility uses lower-case escapes, Uri.EscapeDataString upper-case.
    public Func<string, string> Encode { get; set; } = HttpUtility.UrlEncode;

    public XmlDocument BuildRequest() => Build($"""
        <samlp:LogoutRequest xmlns:samlp="{Saml20Constants.PROTOCOL}" xmlns:saml="{Saml20Constants.ASSERTION}" ID="{Id}" Version="2.0" IssueInstant="{IssueInstant:o}" Destination="{Destination}">
          <saml:Issuer>{Issuer}</saml:Issuer>
          <saml:NameID Format="{NameIdFormat}">{NameId}</saml:NameID>
          <samlp:SessionIndex>session-1</samlp:SessionIndex>
        </samlp:LogoutRequest>
        """);

    public XmlDocument BuildResponse() => Build($"""
        <samlp:LogoutResponse xmlns:samlp="{Saml20Constants.PROTOCOL}" xmlns:saml="{Saml20Constants.ASSERTION}" ID="{Id}" Version="2.0" IssueInstant="{DateTime.UtcNow:o}" Destination="{Destination}" InResponseTo="{InResponseTo}">
          <saml:Issuer>{Issuer}</saml:Issuer>
          {Status()}
        </samlp:LogoutResponse>
        """);

    public TestLogoutParameters PostRequest() => new() { LogoutRequest = Base64(BuildRequest()), RelayState = RelayState };

    public TestLogoutParameters PostResponse() => new() { LogoutResponse = Base64(BuildResponse()), RelayState = RelayState };

    public TestLogoutParameters RedirectRequest() => Redirect(HttpRedirectBindingConstants.SAMLRequest, BuildRequest());

    public TestLogoutParameters RedirectResponse() => Redirect(HttpRedirectBindingConstants.SAMLResponse, BuildResponse());

    private XmlDocument Build(string xml)
    {
        var doc = SecureXml.Parse(xml);
        BeforeSigning?.Invoke(doc);
        if (Sign) RsaSha256Signature.Sign(doc, Id, SigningCertificate);
        return doc;
    }

    private TestLogoutParameters Redirect(string parameterName, XmlDocument message)
    {
        var deflated = Deflate(message.OuterXml);
        var relayState = RelayState == null ? "" : $"&{HttpRedirectBindingConstants.RelayState}={Encode(RelayState)}";
        var signedQuery = $"{parameterName}={Encode(deflated)}{relayState}&{HttpRedirectBindingConstants.SigAlg}={Encode(SigAlg)}";
        using var key = SigningCertificate.GetRSAPrivateKey()!;
        var signature = Sign ? Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(signedQuery), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) : null;
        return new TestLogoutParameters
        {
            Binding = Saml20Constants.ProtocolBindings.HTTP_Redirect,
            LogoutRequest = parameterName == HttpRedirectBindingConstants.SAMLRequest ? deflated : null,
            LogoutResponse = parameterName == HttpRedirectBindingConstants.SAMLResponse ? deflated : null,
            RelayState = RelayState,
            SigAlg = Sign ? SigAlg : null,
            Signature = signature,
            RawQuery = signature == null ? signedQuery : $"{signedQuery}&{HttpRedirectBindingConstants.Signature}={Encode(signature)}",
        };
    }

    private static string Base64(XmlDocument doc) => Convert.ToBase64String(Encoding.UTF8.GetBytes(doc.OuterXml));

    public static string Deflate(string xml)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionMode.Compress, true)) deflate.Write(Encoding.UTF8.GetBytes(xml));
        return Convert.ToBase64String(output.ToArray());
    }

    private string Status() =>
        $"<samlp:Status><samlp:StatusCode Value=\"{StatusCode}\">{(SubStatusCode == null ? "" : $"<samlp:StatusCode Value=\"{SubStatusCode}\"/>")}</samlp:StatusCode>"
        + (StatusMessage == null ? "" : $"<samlp:StatusMessage>{StatusMessage}</samlp:StatusMessage>") + "</samlp:Status>";
}
