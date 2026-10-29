# Logging

The library logs through `Microsoft.Extensions.Logging`, so it works with any provider: the console, Serilog, NLog, OpenTelemetry, Application Insights and so on. It writes two kinds of log:

- **The audit log:** login and logout events that NemLog-in's logging policy requires, under one category, `DK.Gov.Oio.Saml.Audit`.
- **Diagnostic logs:** certificate warnings and failures, under the categories of the classes that write them.

- [The logging policy](#the-logging-policy)
- [Audit events](#audit-events)
- [What the policy requires and where it is logged](#what-the-policy-requires-and-where-it-is-logged)
- [Certificate events](#certificate-events)
- [The client IP address](#the-client-ip-address)
- [Configuring the logs](#configuring-the-logs)
- [What you must do yourself](#what-you-must-do-yourself)

## The logging policy

Service providers connected to NemLog-in must follow the logging policy for service providers (*Logningspolitik for tjenesteudbydere tilsluttet NemLog-in*, v1.5), which is published on NemLog-in's [documentation and guides](https://www.nemlog-in.dk/om-nemlog-in/dokumentation-og-vejledninger/) page. Its appendix A lists the data to log for each login and logout, and requires details of every error, including SAML errors and signature and certificate validation failures.

## Audit events

All audit events are structured: each value is a separate field, named as below, so the log can be searched and events joined by their IDs.

| Event ID | Level       | Event                           | Fields                                                                                                                   |
|----------|-------------|---------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| 1        | Information | `AuthnRequest` sent             | `AuthnRequestId`, `Destination`                                                                                          |
| 2        | Information | Login accepted                  | `ResponseId`, `InResponseTo`, `AssertionId`, `Issuer`, `NameId`, `LevelOfAssurance`, `Privileges`, `LocalSessionId`       |
| 3        | Warning     | Login rejected                  | `ResponseId`, `InResponseTo`, `Reason`, and the exception                                                                |
| 4        | Information | User chose logout               | `LogoutRequestId`, `Destination`, `NameId`, `SessionIndex`, `LocalSessionId` (the local session has ended)                 |
| 5        | Information | `LogoutResponse` accepted       | `LogoutResponseId`, `InResponseTo`, `Status`                                                                             |
| 6        | Warning     | `LogoutResponse` rejected       | `LogoutResponseId`, `InResponseTo`, `Status`, `Reason`, and the exception                                                |
| 7        | Information | IdP's `LogoutRequest` accepted  | `LogoutRequestId`, `Issuer`, `NameId`, `LocalSessionId` (the local session has ended); without `LocalSessionId` when no one was signed in |
| 8        | Warning     | IdP's `LogoutRequest` rejected  | `LogoutRequestId`, `Issuer`, `NameId`, `Reason` (the local session is kept)                                               |

- `LocalSessionId` is a random ID created at login and stored as the `ClaimsConstants.LocalSessionId` claim. It ties the login, the logout and your own log lines for that session together.
- `Reason` is the validation failure, usually citing the OIOSAML requirement, e.g. `[OIO-IDP-12] Invalid Assertion signature: …`.
- A rejected message may lack IDs, for example when it couldn't be parsed; those fields are then empty.

A login accepted at event 2 has passed every check: signatures, decryption, audience, validity windows, `InResponseTo`, replay and level of assurance. A failure in any of them is event 3.

## What the policy requires and where it is logged

| Policy (appendix A)                                         | Logged as                                                                             |
|-------------------------------------------------------------|---------------------------------------------------------------------------------------|
| **5.1** ID of the `AuthnRequest`                            | Event 1, `AuthnRequestId`                                                             |
| **5.1, 5.2** End user's IP address (optional)               | `ClientIp` scope on every event, see [below](#the-client-ip-address)                  |
| **5.2** ID of the response                                  | Event 2 or 3, `ResponseId`                                                            |
| **5.2** Level of assurance in the assertion                 | Event 2, `LevelOfAssurance`                                                           |
| **5.2** `InResponseTo`                                      | Event 2 or 3, `InResponseTo`                                                          |
| **5.2** Result of validating the response and assertion     | Event 2 (accepted) or 3 (rejected, with `Reason`)                                     |
| **5.2** User ID (the assertion's NameID)                    | Event 2, `NameId`                                                                     |
| **5.2** The internal user account the assertion maps to     | **Not logged by the library.** Log it in your application, with `LocalSessionId`       |
| **5.2** Rights and privileges in the assertion              | Event 2, `Privileges`                                                                 |
| **5.2** ID of the local session                             | Event 2, `LocalSessionId`                                                             |
| **5.3** That the user chose logout                          | Event 4                                                                               |
| **5.3** ID, NameID and `SessionIndex` of the `LogoutRequest` | Event 4                                                                               |
| **5.3** Status of NemLog-in's `LogoutResponse`              | Event 5 or 6, `Status`                                                                |
| **5.4** ID of the IdP's `LogoutRequest`                     | Event 7 or 8, `LogoutRequestId`                                                       |
| **5.4** The user logged out                                 | Event 7, `NameId`                                                                     |
| **5.4** That the local session was terminated               | Event 7                                                                               |
| **5.5** Attribute queries                                   | Not applicable: the library doesn't make attribute queries                            |
| Errors, including certificate validation failures           | Events 3, 6, 8, and the [certificate events](#certificate-events)                     |

Appendix B (signing) doesn't apply: the library doesn't use NemLog-in's signing services.

To log the internal user account, write a log line when your application maps the login to a user, for example in the cookie scheme's `OnSignedIn` event or on the first request after login, including the `LocalSessionId` claim:

```csharp
logger.LogInformation("Local session {LocalSessionId} is user account {UserAccount}",
    User.FindFirst(ClaimsConstants.LocalSessionId)?.Value, account.Id);
```

## Certificate events

| Category                                                            | Level   | When                                                                                                    |
|---------------------------------------------------------------------|---------|---------------------------------------------------------------------------------------------------------|
| `DK.Gov.Oio.Saml.Certificates.IdentityProviderCertificateRefresh`   | Warning | An IdP signing certificate is expired, revoked or its revocation status is unknown; also at startup when revocation checking is disabled |
| same                                                                | Error   | The revocation check itself failed                                                                      |
| `DK.Gov.Oio.Saml.Certificates.ServiceProviderCertificateExpiry`     | Warning | An SP certificate expires within 30 days                                                                |
| same                                                                | Error   | An SP certificate has expired                                                                           |

The policy requires certificate validation failures to be logged, so keep these with the audit log. They also deserve alerts; see [certificates](certificates.md).

## The client IP address

The `/auth` endpoints add a `ClientIp` scope to everything logged while they run, including all audit events. Scopes appear only when the provider includes them, e.g. `"IncludeScopes": true` for the console.

Behind a reverse proxy or load balancer, the connection's address is the proxy's. Apply ASP.NET Core's forwarded headers middleware before the endpoints, and limit it to your proxies:

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownProxies.Add(IPAddress.Parse("10.0.0.10"));
});

var app = builder.Build();
app.UseForwardedHeaders();
```

## Configuring the logs

Make sure the audit category is enabled at `Information`, even when other categories log less:

```json
"Logging": {
  "LogLevel": {
    "Default": "Warning",
    "DK.Gov.Oio.Saml": "Information"
  },
  "Console": {
    "FormatterName": "json",
    "FormatterOptions": { "IncludeScopes": true, "UseUtcTimestamp": true }
  }
}
```

`DK.Gov.Oio.Saml` covers the audit and certificate categories. The sample uses this JSON console format, which a log collector can ingest field by field.

Send the audit category to storage that keeps it for **at least six months**. With most providers you filter on the category, for example with Serilog:

```csharp
.WriteTo.Logger(audit => audit
    .Filter.ByIncludingOnly(Matching.FromSource("DK.Gov.Oio.Saml"))
    .WriteTo.YourAuditSink(...))
```

Record timestamps in UTC.

## What you must do yourself

The policy places requirements on the service provider that a library can't meet:

- **Retention:** keep the logs at least six months, then delete them under a documented procedure.
- **Integrity and access:** protect the logs from unauthorised reading, change and deletion, control access to them, and back them up.
- **Time synchronisation:** synchronise the servers' clocks with an accurate source (NTP).
- **Personal data:** the NameID, privileges and IP address are personal data under GDPR. Handle the logs accordingly.
- **Untrusted values:** a rejected message's IDs, issuer, NameID and the reason can come from whoever sent it. The events are structured, with each value in its own field; a sink that writes plain text must encode the values, so a line break in one can't forge a log line.
- **Monitoring:** monitor that logging works.
- **The internal user account:** log it, see [above](#what-the-policy-requires-and-where-it-is-logged).
