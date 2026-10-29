using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.Builders;

internal class HttpRequestInfo(string requestType, string content)
{
    public string RequestType { get; } = requestType;
    public string Content { get; } = content;
    public string? RelayState { get; set; }
    public RSA? SigningKey { get; set; }
}

// The HTTP-Redirect binding, [SAMLBind] 3.4.
internal class HttpRedirectRequestBuilder(OioSamlOptions options)
{
    public string GetSignedAuthnRequest(Saml20AuthnRequest authnRequest, string? relayState) =>
        SignedUrl(authnRequest.Destination, HttpRedirectBindingConstants.SAMLRequest, authnRequest.GetXml().OuterXml, relayState);

    public SignedLogoutRequest GetSignedLogoutRequest(Saml20LogoutRequest logoutRequest, string? relayState) =>
        new(SignedUrl(logoutRequest.Destination, HttpRedirectBindingConstants.SAMLRequest, logoutRequest.GetXml().OuterXml, relayState), isRedirectRequest: true);

    public SignedLogoutResponse GetSignedLogoutResponse(Saml20LogoutResponse logoutResponse, string? relayState) =>
        new(SignedUrl(logoutResponse.Destination, HttpRedirectBindingConstants.SAMLResponse, logoutResponse.GetXml().OuterXml, relayState), isRedirectResponse: true);

    private string SignedUrl(string destination, string messageType, string xml, string? relayState)
    {
        using var key = options.ServiceProvider.GetCertificate().GetRSAPrivateKey();
        return destination + "?" + GetQueryForRequest(new HttpRequestInfo(messageType, xml) { RelayState = relayState, SigningKey = key }).ToQueryString();
    }

    private static List<(string Name, string Value)> GetQueryForRequest(HttpRequestInfo requestInfo)
    {
        var result = new List<(string Name, string Value)> { (requestInfo.RequestType, UpperCaseUrlEncode(HttpUtility.UrlEncode(DeflateEncode(requestInfo.Content)))) };
        if (requestInfo.RelayState != null)
            result.Add((HttpRedirectBindingConstants.RelayState, UpperCaseUrlEncode(HttpUtility.UrlEncode(requestInfo.RelayState))));
        if (requestInfo.SigningKey == null) return result;

        // [SAMLBind] 3.4.4.1: the signature covers the query string as sent, SigAlg included.
        result.Add((HttpRedirectBindingConstants.SigAlg, UpperCaseUrlEncode(HttpUtility.UrlEncode(RsaSha256Signature.SignatureUri))));
        var signature = RsaSha256Signature.SignData(requestInfo.SigningKey, Encoding.UTF8.GetBytes(result.ToQueryString()));
        result.Add((HttpRedirectBindingConstants.Signature, HttpUtility.UrlEncode(Convert.ToBase64String(signature))));
        return result;
    }

    // Some IdPs (Ping) refuse lower-case percent-encoding.
    private static string UpperCaseUrlEncode(string s)
    {
        var result = new StringBuilder(s);
        for (var i = 0; i < result.Length; i++)
        {
            if (result[i] != '%') continue;
            result[++i] = char.ToUpper(result[i]);
            result[++i] = char.ToUpper(result[i]);
        }
        return result.ToString();
    }

    private static string DeflateEncode(string value)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(new DeflateStream(memoryStream, CompressionMode.Compress, true), new UTF8Encoding(false)))
            writer.Write(value);
        return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
    }
}
