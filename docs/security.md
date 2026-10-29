# Security

What the library checks for you, what it deliberately refuses, and what your application must still do.

- [Login responses](#login-responses)
- [Logout messages](#logout-messages)
- [XML and signature handling](#xml-and-signature-handling)
- [Algorithms](#algorithms)
- [Outbound messages](#outbound-messages)
- [Redirects and RelayState](#redirects-and-relaystate)
- [Your application's responsibilities](#your-applications-responsibilities)
- [Known limitations](#known-limitations)

## Login responses

A response to `/auth/AssertionConsumerService` is accepted only if every check below passes; the first failure rejects it with a `Saml20ValidationException` and an [audit event](logging.md#audit-events). Requirement IDs refer to the OIOSAML Web SSO Profile.

**The response**
- It answers an `AuthnRequest` this browser session sent: a pending request is in the session and `InResponseTo` matches its ID. The pending request is removed on first use.
- `Destination` is exactly the assertion consumer service URL.
- The status is `Success`.
- It holds exactly one `EncryptedAssertion` and no plaintext assertion ([OIO-IDP-11], [OIO-IDP-13]).
- If the response itself is signed, the signature is valid.

The checks above that need no decryption run first, so an unsolicited or misdirected response is refused before the SP's private key is used on it.

**The assertion**
- It is signed by the IdP, and the signature is valid ([OIO-IDP-12]). The assertion must be signed even though the response may not be.
- Its `Issuer` is the configured IdP, and the same as the response's.
- The `Subject` has exactly one transient or persistent `NameID`, and a bearer `SubjectConfirmation` whose `Recipient` is the assertion consumer service URL, whose `InResponseTo` matches the request, and whose validity window includes the current time ([OIO-IDP-15], [OIO-IDP-17]).
- `Conditions` are present, the current time is within them, and the `AudienceRestriction` includes the SP's entity ID ([OIO-IDP-18]).
- There is exactly one `AuthnStatement` and one `AttributeStatement` ([OIO-IDP-11]); attributes use the URI name format and aren't encrypted ([OIO-AP-03]).
- With `ForceAuthn`, `AuthnInstant` isn't older than the request ([OIO-SP-15]).
- The level of assurance is at least the one requested or configured ([OIO-SP-16]), and, when the login asked for attribute profiles, the returned profile is one of them ([OIO-SP-07]).
- Its ID hasn't been accepted before (replay protection), until the assertion expires.

Time comparisons allow `allowedClockSkew` (3 minutes by default).

## Logout messages

`LogoutRequest`s and `LogoutResponse`s on `/auth/logout`, by HTTP-Redirect or HTTP-POST:

- They must be signed by the IdP, with the signature checked as for assertions. On the redirect binding the signature is verified over the query string exactly as the IdP sent it; a repeated SAML parameter, in any spelling, is refused.
- `Issuer` is the configured IdP. A `LogoutRequest` from any other issuer gets no answer at all.
- `Destination` is exactly the single logout service URL.
- A `LogoutResponse` must answer the `LogoutRequest` this session sent.
- A `LogoutRequest` must name the signed-in user with a plaintext `NameID` ([OIO-IDP-24], [OIO-SP-23]); otherwise it's answered `RequestDenied` and the local session is kept.
- A `LogoutRequest`'s `IssueInstant` must be at most 5 minutes old and not in the future (plus `allowedClockSkew`), and its `NotOnOrAfter`, if any, not passed ([OIO-GE-01]). The IdP sends a request as soon as it issues it, so an older one may be a replay.
- A redirect-binding message may inflate to at most 256 KB; anything larger, or anything that isn't base64-encoded XML, is refused with a `Saml20FormatException`.

SP-initiated logout ends the local session before the request is sent ([OIO-SP-28]). Your application starts it only with a `POST` carrying a valid antiforgery token, so another site can't log a user out. The IdP's messages, which arrive cross-site without a token, are exempt; their signature protects them. The `/auth` endpoints are therefore also exempt from a global `AutoValidateAntiforgeryTokenAttribute`.

## XML and signature handling

- **No DTDs and no external resolution** ([OIO-GE-02]), in messages and in the IdP metadata file alike. This blocks XML external entity (XXE) and entity expansion attacks.
- **Nesting and size limits:** a message or metadata file nested more than 64 levels deep is refused before it is parsed into a document, as parsing recurses once per level and a stack overflow would end the process. The `/auth` endpoints refuse a posted message over 1 MB, and a redirect-binding message that inflates beyond 256 KB. Real SAML messages stay far below all three.
- **Signature wrapping:** a signature is accepted only when it is a direct child of the element it signs and its reference points to that element's own `ID`. The reference resolver only resolves that ID, so a signed copy placed elsewhere in the document can't satisfy it.
- **Keys** come only from the IdP's metadata, never from the `KeyInfo` in the message.
- **IdP certificates** must be within their validity period and not revoked; see [revocation checking](certificates.md#revocation-checking).

## Algorithms

Inbound messages must use the algorithms of [OIO-ALG-01], with one restriction:

| Use                 | Allowed                                                                                     |
|---------------------|---------------------------------------------------------------------------------------------|
| Signature           | RSA-SHA256                                                                                  |
| Digest              | SHA-256                                                                                     |
| Canonicalization    | Canonical XML 1.0, Exclusive Canonical XML                                                  |
| Transforms          | Enveloped signature, Canonical XML 1.0, Exclusive Canonical XML                             |
| Key transport       | RSA-OAEP (XML Encryption 1.0 and 1.1)                                                       |
| Content encryption  | AES-128-GCM, AES-192-GCM, AES-256-GCM                                                       |

**AES-CBC is refused**, although [OIO-ALG-01] allows it, because AES-GCM is more secure. Don't list AES-CBC in your SP metadata; NemLog-in uses AES-GCM by default.

## Outbound messages

- `AuthnRequest`s and logout messages are signed with RSA-SHA256 by the SP certificate.
- Message IDs are random (122 bits), and the pending request is kept in the server-side session, bound to the browser's session cookie.

## Redirects and RelayState

- **After login** the user goes to the `ReturnUrl` stored when the login started, only if it is a local URL (same rules as ASP.NET Core's `IsLocalUrl`), otherwise to `serviceProvider:loginRedirectUrl`. The redirect target never comes from the IdP's response.
- **RelayState**, at login and at logout, is never sent to the IdP. The IdP receives a random token; your value stays in the session and is handed back only when the exact token returns.

## Your application's responsibilities

- **HTTPS everywhere**, with HSTS. The cookies are `Secure` and won't be sent over HTTP.
- **Data protection keys:** the authentication cookie is encrypted with ASP.NET Core data protection. Persist and protect the keys, and share them between instances.
- **Shared caches across instances:** the session and the replay cache must be shared, or replay protection covers only one instance. See [running more than one instance](integration.md#running-more-than-one-instance).
- **Session lifetime:** keep the [NemLog-in limits](integration.md#session-lifetime).
- **Login requirements come from the configuration:** the level of assurance, profiles, `forceAuthn` and `isPassive` are configured, and the configured minimum level is a floor that no request can lower ([configuring what a login requires](integration.md#configuring-what-a-login-requires)). Leave `login:allowParametersFromRequest` off in production: it lets the user choose them, which only a test SP should allow.
- **CSRF protection for your own endpoints:** the login cookie is `SameSite=None`, because the IdP's logout arrives cross-site, so browsers send it with requests from other sites too. Validate an antiforgery token on every state-changing request: on ASP.NET Core with a global `AutoValidateAntiforgeryTokenAttribute`, on MVC 5 with `[ValidateAntiForgeryToken]` ([details](integration.md#csrf-protection-for-your-own-endpoints)).
- **Authorization:** the library only authenticates. Check the claims a page needs, such as the level of assurance or a privilege, before granting access to it.
- **Secrets:** keep certificate passwords and keys out of source control and `appsettings.json`.
- **Forwarded headers:** behind a proxy, configure `UseForwardedHeaders` for your proxies only, so client IP addresses in the audit log can't be spoofed.
- **Clock:** synchronise the server clock (NTP); validity windows depend on it.
- **Logging:** keep and protect the audit log; see [logging](logging.md).

## Known limitations

- **OneTimeUse** in an assertion's `Conditions` isn't acted on; replay protection already prevents an assertion from being used twice. An SP can ask for it with [`identityProvider:oneTimeUse`](configuration.md#one-time-use).
