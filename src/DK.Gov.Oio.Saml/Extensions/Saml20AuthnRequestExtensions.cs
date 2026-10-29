using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Extensions;

internal static class Saml20AuthnRequestExtensions
{
    public static List<string> GetAudienceRestrictions(this Saml20AuthnRequest request) =>
        request.Request?.Conditions?.Items?.OfType<AudienceRestriction>().SelectMany(x => x.Audience).ToList() ?? [];

    public static IEnumerable<string> GetAttributeProfiles(this AuthnRequest authnRequest) =>
        authnRequest.Extensions?.Any?.SingleOrDefault(e => e.LocalName == "RequestedAttributeProfiles")?.ChildNodes.Cast<XmlNode>()
            .Where(n => !string.IsNullOrEmpty(n.InnerText)).Select(n => n.InnerText) ?? [];

    extension(Status status)
    {
        public string GetErrorMessage() =>
            status.StatusCode?.Value == Saml20Constants.StatusCodes.Responder && status.StatusCode.SubStatusCode?.Value == Saml20Constants.StatusCodes.NoPassive
                ? "IdP responded with status code NoPassive. A user cannot be signed in with the IsPassive flag set when the user does not have a session with the IdP."
                : $"ErrorCode: {status.StatusCode?.Value}{(status.StatusCode?.SubStatusCode?.Value is { } sub ? $" ({sub})" : "")}. Message: {status.StatusMessage}.";

        public bool IsSuccess() => status.StatusCode?.Value == Saml20Constants.StatusCodes.Success;
    }

    extension(Assertion assertion)
    {
        public NameID GetSubject() => assertion.Subject.Items.OfType<NameID>().Single();

        public SubjectConfirmationData? GetBearerConfirmationData() =>
            assertion.Subject?.Items?.OfType<SubjectConfirmation>().FirstOrDefault(c => c.Method == SubjectConfirmation.BEARER_METHOD)?.SubjectConfirmationData;

        public IEnumerable<SamlAttribute> GetAttributes() =>
            assertion.GetAttributeStatements().SelectMany(x => x.Items?.OfType<SamlAttribute>() ?? []);

        public IEnumerable<string> GetAttributeValues(string attributeName) =>
            assertion.GetAttributes().Where(a => a.Name == attributeName).SelectMany(a => a.AttributeValue ?? []);

        public ClaimsPrincipal ToClaimsPrincipal(string localSessionId)
        {
            var subject = assertion.GetSubject();
            var identity = new GenericIdentity(subject.Value, Saml20Constants.AuthenticationScheme);
            identity.AddClaims(assertion.GetAttributes().SelectMany(a => (a.AttributeValue ?? []).Where(v => v != null).Select(v => new Claim(a.Name, v))));
            identity.AddClaims([
                new Claim(ClaimsConstants.SessionIndex, assertion.GetAuthnStatements().FirstOrDefault()?.SessionIndex ?? string.Empty),
                new Claim(ClaimsConstants.Issuer, assertion.Issuer.Value),
                new Claim(ClaimsConstants.SubjectNameId, subject.Value),
                new Claim(ClaimsConstants.SubjectNameIdFormat, subject.Format),
                new Claim(ClaimsConstants.LocalSessionId, localSessionId),
            ]);
            return new ClaimsPrincipal(identity);
        }
    }

    extension(XmlElement element)
    {
        public Status? GetStatus()
        {
            var status = element.GetSingleChild(Status.ELEMENT_NAME, Saml20Constants.PROTOCOL);
            return status == null ? null : Serialization.Deserialize<Status>(new XmlNodeReader(status));
        }

        public string? GetIssuerValue() => element.GetSingleChild("Issuer")?.InnerText;

        public XmlElement? GetSingleChild(string localName, string ns = Saml20Constants.ASSERTION)
        {
            var children = element.GetChildren(localName, ns);
            return children.Count == 1 ? children[0] : null;
        }

        public List<XmlElement> GetChildren(string localName, string ns = Saml20Constants.ASSERTION) =>
            element.ChildNodes.OfType<XmlElement>().Where(c => c.LocalName == localName && c.NamespaceURI == ns).ToList();
    }

    extension(ILogoutParameters p)
    {
        public bool IsSpInitiatedLogout() => p.LogoutRequest == null && p.LogoutResponse == null;

        public bool IsIdPInitiatedLogout() => p.LogoutRequest != null && p.LogoutResponse == null;

        public bool IsLogoutResponse() => p.LogoutRequest == null && p.LogoutResponse != null;
    }
}
