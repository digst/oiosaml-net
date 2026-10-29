# Configuration reference

All settings live in one type, `OioSamlOptions`. It's usually bound from an `oiosaml` configuration section, but can be built or adjusted in code (see [registering the services](integration.md#registering-the-services)). JSON keys are camelCase and match the C# property names; both forms work.

- [Complete example](#complete-example)
- [serviceProvider](#serviceprovider)
- [Service provider certificate](#service-provider-certificate)
- [identityProvider](#identityprovider)
- [levelOfAssurance](#levelofassurance)
- [login](#login)
- [certificateRevocation](#certificaterevocation)
- [session](#session)
- [allowedClockSkew](#allowedclockskew)
- [appSwitchReturnUrls](#appswitchreturnurls)
- [Startup validation](#startup-validation)
- [One configuration per environment](#one-configuration-per-environment)
- [Paths and secrets](#paths-and-secrets)

## Complete example

```json
{
  "oiosaml": {
    "serviceProvider": {
      "entityId": "https://saml.my-service.example.dk",
      "server": "https://my-service.example.dk",
      "loginRedirectUrl": "/",
      "logoutRedirectUrl": "/",
      "certificate": { "filePath": "certificates/sp.p12" }
    },
    "identityProvider": {
      "entityId": "https://saml.test-devtest4-nemlog-in.dk",
      "metadataFile": "idp-metadata/devtest4-idp-metadata.xml",
      "forceAuthn": false,
      "isPassive": false,
      "oneTimeUse": false
    },
    "levelOfAssurance": {
      "requestGenericLoa": true,
      "minimumLoa": "Substantial",
      "requestMinimumLoa": true
    },
    "login": {
      "attributeProfiles": [ "https://data.gov.dk/eid/Person/DK", "https://data.gov.dk/eid/Person/DK/Anonymous" ]
    },
    "certificateRevocation": {
      "enabled": true,
      "checkInterval": "00:10:00",
      "gracePeriod": "04:00:00"
    },
    "session": {
      "idleTimeout": "00:30:00",
      "maximumLength": "08:00:00"
    },
    "allowedClockSkew": "00:03:00",
    "appSwitchReturnUrls": [
      { "platform": "Android", "value": "https://my-service.example.dk/app/android" },
      { "platform": "iOS", "value": "https://my-service.example.dk/app/ios" }
    ]
  }
}
```

The certificate password is deliberately missing; see [paths and secrets](#paths-and-secrets).

## serviceProvider

Your own service provider.

| Key                           | Required                            | Default                                  | Description                                                                                                                                  |
|-------------------------------|-------------------------------------|------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| `entityId`                    | yes                                 |                                          | The SP's entity ID, exactly as in its metadata. Sent as `Issuer` and checked against the assertion's `Audience`                              |
| `server`                      | yes, unless both URLs below are set |                                          | Base URL of the application, e.g. `https://my-service.example.dk`. The endpoint URLs below are built from it                                 |
| `assertionConsumerServiceUrl` | no                                  | `{server}/auth/AssertionConsumerService` | Where the IdP posts the login response. Must match the SP metadata byte for byte, because `Destination` and `Recipient` are compared exactly |
| `singleLogoutServiceUrl`      | no                                  | `{server}/auth/logout`                   | Where logout messages arrive. Must also match the metadata exactly                                                                           |
| `loginRedirectUrl`            | no                                  | `/`                                      | Where the user goes after login when there's no local `ReturnUrl`                                                                            |
| `logoutRedirectUrl`           | no                                  | `/`                                      | Where the user goes after an SP-initiated logout                                                                                             |
| `certificate`                 | yes (here or in code)               |                                          | The SP's key material; see [below](#service-provider-certificate)                                                                            |

Set the two URLs explicitly when the public address differs from `server`'s pattern, for example behind a proxy that rewrites paths.

## Service provider certificate

The SP certificate's private key signs `AuthnRequest`s and logout messages, and decrypts the assertions the IdP encrypts to it. It must be the certificate in your SP metadata, normally an OCES organisation certificate. NemLog-in holds one certificate per SP, so `serviceProvider:certificate` is a single entry, in one of these forms. [Certificates](certificates.md) covers formats, expiry and replacing it.

| Source                    | Keys                                                                              | Remarks                                                                                                                                                                                                                                           |
|---------------------------|-----------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| PKCS#12 file              | `filePath`, `password`                                                            | Works on every platform. The key is kept in memory, except on macOS, where the OS requires a temporary keychain                                                                                                                                   |
| PEM files                 | `filePath` (certificate), `keyFilePath` (key), `password` if the key is encrypted | Recognised by the `-----BEGIN` header. Without `keyFilePath`, the key is read from the same file. PKCS#1 and PKCS#8 RSA keys, plain or encrypted, are accepted, and the key must belong to the certificate. Suits containers with mounted secrets |
| Windows certificate store | `thumbprint`, `storeLocation` (`CurrentUser` or `LocalMachine`)                   | Windows only. `storeLocation` is required. The process identity needs access to the private key. Copying a thumbprint from the MMC certificate snap-in often adds an invisible character at the start; the library detects this and says so       |
| Code                      | `options.ServiceProvider.SetCertificate(X509Certificate2)`                        | For keys from a vault or a custom source. A certificate set in code takes precedence over `serviceProvider:certificate`, and it must carry its private key                                                                                         |

**Checked at startup:**
- A certificate is configured.
- It has an accessible private key.
- It hasn't expired. An expired certificate stops the application with its expiry date.

**Checked while running:** once a day, a background service logs a **warning** when the certificate expires within 30 days, and an **error** once it has expired.

## identityProvider

The IdP the SP federates with. The library supports exactly one, as the OIOSAML.Java reference implementation does.

| Key            | Required | Default | Description                                                                                                                                                                                                                              |
|----------------|----------|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `entityId`     | yes      |         | The IdP's entity ID. Responses and logout messages must come from this issuer                                                                                                                                                            |
| `metadataFile` | yes      |         | The IdP's SAML metadata, downloaded from the NemLog-in administration portal. Its `entityID` must equal `entityId`. The file is read on every use, so replacing it (e.g. for an IdP certificate rollover) takes effect without a restart |
| `forceAuthn`   | no       | `false` | Ask the IdP to authenticate the user again on every login                                                                                                                                                                                |
| `isPassive`    | no       | `false` | Ask the IdP not to interact with the user                                                                                                                                                                                                |
| `oneTimeUse`   | no       | `false` | Add `OneTimeUse` to the `AuthnRequest`'s `Conditions`, so NemLog-in creates no session for the service. Requires `forceAuthn`. See [one-time use](#one-time-use)                                                                         |

The IdP's single sign-on and logout endpoints and its signing certificates come from the metadata. Only `KeyDescriptor`s marked `use="signing"`, or without `use`, are trusted to verify signatures.

Take `entityId` from the `entityID` attribute of the metadata you download. DevTest4's is `https://saml.test-devtest4-nemlog-in.dk`. If the two differ, the startup check stops the application and says so.

### One-time use

[OIO-SP-18] lets an SP that uses features like `OneTimeUse` to stay out of the IdP's sessions and exempt the service from conforming to the Single Logout Profile. With `identityProvider:oneTimeUse`, NemLog-in authenticates the user without creating a session for the service:

- **Set `forceAuthn` to `true` as well.** NemLog-in requires it with `OneTimeUse`.
- **Login works**, and the local login session is created as usual.
- **Logout through NemLog-in fails**, as NemLog-in holds no session to end. End the session locally instead, with `HttpContext.SignOutAsync(Saml20Constants.AuthenticationScheme)`, rather than posting to `/auth/logout`.

The setting only changes the request. An assertion is accepted whether or not it carries `OneTimeUse`; [replay protection](security.md#login-responses) already stops it being used twice.

## levelOfAssurance

| Key                 | Default       | Description                                                                                                                                                                                            |
|---------------------|---------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `minimumLoa`        | `Substantial` | The lowest level a login is accepted at. Applies to both NSIS and eIDAS levels unless one of the keys below overrides it                                                                                |
| `minimumNsisLoa`    | `minimumLoa`  | Override for responses that carry an NSIS level                                                                                                                                                        |
| `minimumEidasLoa`   | `minimumLoa`  | Override for responses that carry an eIDAS level (EU logins through NemLog-in)                                                                                                                         |
| `requestMinimumLoa` | `true`        | Ask the IdP for the minimum in every `AuthnRequest`, so the user authenticates at that level rather than being refused afterwards. The response is checked against the minimum either way               |
| `requestGenericLoa` | `true`        | How a requested level is expressed. `true` sends the OIOSAML 4 generic class ref (`https://data.gov.dk/concept/core/loa/<Level>`) with `minimumLoa`; `false` sends the OIOSAML 3 NSIS class ref (`https://data.gov.dk/concept/core/nsis/loa/<Level>`) with the NSIS minimum |

Levels are `Low`, `Substantial` and `High`.

**How a response is checked** ([OIO-SP-16]):
- **Which level counts:** the response's NSIS level, or, when it has none, its eIDAS level. The generic level isn't used.
- **The minimum:** the configured minimum is a floor. A login that asked for a higher level, for example a step-up through an [`IAuthnRequestProcessor`](integration.md#requiring-more-for-one-login), must meet that higher level; a request for a lower one is refused before it is sent.
- A response with neither an NSIS nor an eIDAS level is rejected.

## login

What every login asks the IdP for, besides the level of assurance above and `forceAuthn`/`isPassive` under [identityProvider](#identityprovider). Configure it here rather than in code; see [configuring what a login requires](integration.md#configuring-what-a-login-requires).

| Key                          | Default | Description                                                                                                                                                                                                                                      |
|------------------------------|---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `profile`                    |         | OIOSAML 3: `Person` or `Professional`. Requested as `https://data.gov.dk/eid/<Profile>`; the NameID must then be a person or professional identifier                                                                                              |
| `attributeProfiles`          | `[]`    | OIOSAML 4: the attribute profiles to request, from `OioSaml4AttributeProfiles`. Request a profile that has an `/Anonymous` variant together with that variant, e.g. `https://data.gov.dk/eid/Person/DK` and `https://data.gov.dk/eid/Person/DK/Anonymous`; NemLog-in refuses it alone with NLIDP-116 "Anonymous attribute profile variant is missing". The response must carry one of them ([OIO-SP-07])                                                                         |
| `allowParametersFromRequest` | `false` | **For test SPs only.** `/auth/login` then also takes the level of assurance, profiles, `forceAuthn` and `isPassive` from its [query parameters](integration.md#login-parameters-for-test-sps), which the user controls. A level below the minimum is still refused |

## certificateRevocation

How the IdP's signing certificates are checked.

| Key             | Default    | Description                                                                                                                                                                                                                      |
|-----------------|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `enabled`       | `true`     | Check revocation with OCSP (by POST), then fall back to the CRL. When `false`, only the validity period is checked, and a warning is logged at startup. Turn it off only for test IdPs whose revocation services you can't reach |
| `checkInterval` | `00:10:00` | How often a background service re-checks the certificates. Logins use the latest result, so they never wait for OCSP or CRL                                                                                                      |
| `gracePeriod`   | `04:00:00` | How long a certificate that was last seen valid stays accepted while its revocation status can't be determined (e.g. the OCSP responder is down)                                                                                 |

Revoked or expired certificates are dropped at once. A certificate never seen as valid isn't accepted. When no signing certificate is left, logins fail with an error naming the IdP.

## session

How long a login lasts at your service. The limits come from NemLog-in's technical requirements for services (*Tekniske krav til Tjenesters anvendelse af NemLog-in*, §3.2.1), published on NemLog-in's [documentation and guides](https://www.nemlog-in.dk/om-nemlog-in/dokumentation-og-vejledninger/) page.

| Key             | Default    | Description                                                                                                                                                                                  |
|-----------------|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `idleTimeout`   | `00:30:00` | The user is signed out after this long without a request. NemLog-in allows at most 50 minutes and recommends 30; a larger value stops the application at startup                             |
| `maximumLength` | `08:00:00` | The user is signed out this long after login, however active. NemLog-in allows more than 8 hours only when the user is active throughout and there's a concrete business need that requires it |

See [session lifetime](integration.md#session-lifetime) for how they are applied.

## allowedClockSkew

| Key                | Default    | Description                                                                                                                                                 |
|--------------------|------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `allowedClockSkew` | `00:03:00` | Tolerance for clock differences with the IdP. It applies to assertion and logout validity windows and to certificate validity periods. Must not be negative |

Keep the server clock synchronised (NTP) rather than raising this value.

## appSwitchReturnUrls

Needed only when logins use the `appSwitchPlatform` parameter (OIOSAML AppSwitch). 

These are only to be used when your service is a mobile app. The effect of setting appSwitchReturnUrls is that they are passed on to MitID by NemLog-in IdP which causes same-device authentications to make MitID app return to the
app switch return URL after user is finished authenticating with MitID app.

Each entry is a `platform` (`Android` or `iOS`) and its return URL in `value`, every entry needs a `value`. A login asking for a platform without a value is refused.

## Startup validation

`AddOioSaml` calls `OioSamlOptions.Validate()`, which collects every problem and throws one `ConfigurationErrorsException` listing them all:

- `serviceProvider:entityId` is set, and `serviceProvider:server` is set unless both endpoint URLs are.
- `identityProvider:entityId` and `identityProvider:metadataFile` are set. The file exists, is well-formed metadata with an `IDPSSODescriptor` and a `SingleSignOnService`, every endpoint has a `Location`, and its `entityID` is the configured one.
- `allowedClockSkew` is not negative.
- The levels of assurance are known values.
- Every `appSwitchReturnUrls` entry has a `value`.
- `certificateRevocation:checkInterval` is positive and `gracePeriod` is not negative.
- `session:idleTimeout` is positive and at most 50 minutes, and `session:maximumLength` is positive.
- The SP certificate loads, has a private key, and hasn't expired.

You can call `options.Validate()` yourself, for example in a health check or a test.

## One configuration per environment

There's one IdP per configuration. Switch between DevTest4, the integration test environment and production with ordinary ASP.NET Core configuration layering, e.g. `appsettings.Production.json` or environment variables. A typical per-environment file holds:

- the `identityProvider` entity ID and metadata file,
- the SP `entityId` and `server`, if they differ,
- the certificate.

The sample layers `appsettings.oiosaml3.json` or `appsettings.oiosaml4.json` on top of `appsettings.json` in the same way. Each has its own SP entity ID and IdP metadata file.

## Paths and secrets

**Relative paths** (`metadataFile`, `filePath`, `keyFilePath`) are resolved against the application's own folder (`AppContext.BaseDirectory`), not the process's working directory. This works the same when the app runs as a Windows service, in IIS, or in a container. Make sure the files are copied to the output, as the sample does with `<None Update="idp-metadata\**\*.xml" CopyToOutputDirectory="PreserveNewest" />`.

**Certificate passwords** don't normally belong in `appsettings.json`. Use one of these instead:
- user secrets in development,
- an environment variable in deployment: `oiosaml__serviceProvider__certificate__password`;
- a secret store such as Azure Key Vault, added as a configuration provider;
- loading the certificate yourself and setting it with `SetCertificate`.
