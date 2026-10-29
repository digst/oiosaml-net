using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Cryptography;
using DK.Gov.Oio.Saml.Properties;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.Builders;

internal class HttpPostRequestBuilder(OioSamlOptions options)
{
    private static readonly Regex Placeholder = new(@"\{([A-Z]+)\}");

    public SignedLogoutRequest GetSignedLogoutRequest(Saml20LogoutRequest logoutRequest, string? relayState)
    {
        var singedLogoutRequest = logoutRequest.GetXml();
        var submitPage = GetSignedContent(singedLogoutRequest, logoutRequest.ID, logoutRequest.Destination,
            HttpRedirectBindingConstants.SAMLRequest, relayState);

        return new SignedLogoutRequest(submitPage, isRedirectRequest: false);
    }

    public SignedLogoutResponse GetSignedLogoutResponse(Saml20LogoutResponse logoutResponse, string? relayState)
    {
        var signedLogoutResponse = logoutResponse.GetXml();
        var submitPage = GetSignedContent(signedLogoutResponse, logoutResponse.ID, logoutResponse.Destination,
            HttpRedirectBindingConstants.SAMLResponse, relayState);

        return new SignedLogoutResponse(submitPage, isRedirectResponse: false);
    }

    private string GetSignedContent(XmlDocument doc, string id, string destination, string messageType, string? relayState)
    {
        var signingCertificate = options.ServiceProvider.GetCertificate();
        RsaSha256Signature.Sign(doc, id, signingCertificate);

        var values = new Dictionary<string, string>
        {
            ["LOCATION"] = WebUtility.HtmlEncode(destination),
            ["MESSAGETYPE"] = messageType,
            ["MESSAGE"] = WebUtility.HtmlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(doc.OuterXml))),
            ["RELAYSTATEINPUT"] = relayState == null ? "" : $"<input type=\"hidden\" name=\"{HttpRedirectBindingConstants.RelayState}\" value=\"{WebUtility.HtmlEncode(relayState)}\" />",
        };
        return Placeholder.Replace(Resources.HTTPPostSubmitPage, m => values.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);
    }
}
