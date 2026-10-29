using System;
using Microsoft.Extensions.Logging;

namespace DK.Gov.Oio.Saml.Logging;

// The events the NemLog-in logging policy (Logningspolitik for tjenesteudbydere, appendix A) requires, under one category
// so the host can route them to a log it keeps for at least six months.
internal partial class AuditLog(ILoggerFactory loggerFactory)
{
    public const string Category = "DK.Gov.Oio.Saml.Audit";

    private readonly ILogger _logger = loggerFactory.CreateLogger(Category);

    [LoggerMessage(1, LogLevel.Information, "AuthnRequest {AuthnRequestId} sent to {Destination}")]
    public partial void AuthnRequestSent(string authnRequestId, string destination);

    [LoggerMessage(2, LogLevel.Information, "Login accepted: Response {ResponseId} to AuthnRequest {InResponseTo}, Assertion {AssertionId} from {Issuer} for {NameId} at LoA {LevelOfAssurance} with privileges {Privileges}; local session {LocalSessionId}")]
    public partial void LoginAccepted(string responseId, string inResponseTo, string assertionId, string issuer, string nameId, string? levelOfAssurance, string privileges, string localSessionId);

    [LoggerMessage(3, LogLevel.Warning, "Login rejected: Response {ResponseId} to AuthnRequest {InResponseTo}: {Reason}")]
    public partial void LoginRejected(string? responseId, string? inResponseTo, string reason, Exception exception);

    [LoggerMessage(4, LogLevel.Information, "User chose logout: LogoutRequest {LogoutRequestId} sent to {Destination} for {NameId} with SessionIndex {SessionIndex}; local session {LocalSessionId} terminated")]
    public partial void LogoutRequestSent(string logoutRequestId, string destination, string? nameId, string? sessionIndex, string? localSessionId);

    [LoggerMessage(5, LogLevel.Information, "LogoutResponse {LogoutResponseId} to LogoutRequest {InResponseTo} accepted with status {Status}")]
    public partial void LogoutResponseAccepted(string logoutResponseId, string inResponseTo, string? status);

    [LoggerMessage(6, LogLevel.Warning, "LogoutResponse {LogoutResponseId} to LogoutRequest {InResponseTo} rejected with status {Status}: {Reason}")]
    public partial void LogoutResponseRejected(string? logoutResponseId, string? inResponseTo, string? status, string reason, Exception exception);

    [LoggerMessage(7, LogLevel.Information, "LogoutRequest {LogoutRequestId} from {Issuer} for {NameId} accepted; local session {LocalSessionId} terminated")]
    public partial void LogoutRequestAccepted(string logoutRequestId, string issuer, string? nameId, string localSessionId);

    [LoggerMessage(7, LogLevel.Information, "LogoutRequest {LogoutRequestId} from {Issuer} for {NameId} accepted; no local session")]
    public partial void LogoutRequestAcceptedWithoutSession(string logoutRequestId, string issuer, string? nameId);

    [LoggerMessage(8, LogLevel.Warning, "LogoutRequest {LogoutRequestId} from {Issuer} for {NameId} rejected; local session kept: {Reason}")]
    public partial void LogoutRequestRejected(string? logoutRequestId, string? issuer, string? nameId, string reason);
}
