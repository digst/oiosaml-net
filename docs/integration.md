# Integrating with ASP.NET Core

This guide shows how to add NemLog-in login and logout to an ASP.NET Core application with `DK.Gov.Oio.Saml.AspNetCore`. The settings themselves are described in the [configuration reference](configuration.md). The [sample service provider](../samples/DK.Gov.Oio.Saml.TestServiceProvider/Program.cs) is a complete, working setup.

- [Overview](#overview)
- [Registering the services](#registering-the-services)
  - [Session lifetime](#session-lifetime)
- [Middleware](#middleware)
- [Endpoints](#endpoints)
- [Logging in](#logging-in)
- [The signed-in user](#the-signed-in-user)
- [Logging out](#logging-out)
- [Registering the service provider at NemLog-in](#registering-the-service-provider-at-nemlog-in)
- [Running more than one instance](#running-more-than-one-instance)
- [Errors](#errors)
- [Audit logging](#audit-logging)
- [Extension points](#extension-points)

## Overview

1. User opens a page that requires authentication. The cookie handler sends them to `/auth/login`.
2. The library builds an `AuthnRequest`, signs it, stores its ID in the session, and redirects the browser to the IdP, passing the `AuthnRequest` and it's signature as request parameters
3. The user logs in at NemLog-in. The IdP posts a response with an encrypted, signed assertion to `/auth/AssertionConsumerService`.
4. The library decrypts and validates the assertion against the pending request, signs the user in with an authentication cookie, and redirects to the page they came from.
5. Logout goes through `/auth/logout` in both directions, whether initiated by the SP or by the IdP.

The library keeps only small pieces of state on the server: the pending request IDs and RelayState values in the session, and accepted assertion IDs in a replay cache. The user's identity lives in the authentication cookie.

## Registering the services

`AddOioSaml` registers everything the endpoints need from one `OioSamlOptions`. It calls `OioSamlOptions.Validate()` first, so a wrong configuration stops the application at startup, with every problem listed ([what is checked](configuration.md#startup-validation)).

```csharp
// From a configuration section (most common)
builder.Services.AddOioSaml(builder.Configuration.GetSection("oiosaml"));

// From a section, adjusted in code, e.g. a certificate from a vault
builder.Services.AddOioSaml(builder.Configuration.GetSection("oiosaml"),
    options => options.ServiceProvider.SetCertificate(certificateFromVault));

// Entirely in code
builder.Services.AddOioSaml(options =>
{
    options.ServiceProvider.EntityId = "https://saml.my-service.example.dk";
    options.ServiceProvider.Server = "https://my-service.example.dk";
    options.ServiceProvider.SetCertificate(certificate);
    options.IdentityProvider.EntityId = "https://saml.test-devtest4-nemlog-in.dk";
    options.IdentityProvider.MetadataFile = "idp-metadata/devtest4-idp-metadata.xml";
});
```

Add the cookie authentication scheme the library signs users in with, and the session it relies on:

```csharp
builder.Services
    .AddOioSaml(builder.Configuration.GetSection("oiosaml"))
    .AddOioSamlCookieAuthentication();

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())); // see "CSRF protection"
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();      // backs the session; see "Running more than one instance"
builder.Services.AddSession(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;     // the IdP posts the response from another site
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
```

`AddOioSamlCookieAuthentication` registers a cookie scheme named `OIOSamlAssertion` (`Saml20Constants.AuthenticationScheme`). It sets the login path to `/auth/login`, applies the [session lifetime](#session-lifetime), and makes the cookie `SameSite=None`, `Secure` and `HttpOnly`.
- **Several authentication schemes:** if your application registers others, name this one in `[Authorize(AuthenticationSchemes = Saml20Constants.AuthenticationScheme)]` or make it the default.

> **Why the session cookie must be `SameSite=None`:** the IdP returns the user to your site with a cross-site `POST`. Browsers send a `Lax` or `Strict` cookie only on same-site requests, so the session would be missing on the assertion consumer service, and the login would fail with "No pending AuthnRequest found in session".

### CSRF protection for your own endpoints

The login cookie is `SameSite=None` as well. The IdP starts a logout from another site, with a cross-site request to `/auth/logout`, and the library needs the login cookie there to know whom to log out. Browsers therefore send the cookie with cross-site requests to the rest of your application too, and `SameSite` no longer guards against cross-site request forgery.

Validate an antiforgery token on every state-changing request. The simplest way is a global filter; the `/auth` endpoints opt out of it, because the IdP posts to them:

```csharp
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
```

It checks `POST`, `PUT`, `PATCH` and `DELETE`, so don't change state on a `GET`. Razor forms with `method="post"` include the token automatically. For Minimal APIs or a single-page application, see [ASP.NET Core's antiforgery documentation](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery).

### Session lifetime

NemLog-in's technical requirements for services (*Tekniske krav til Tjenesters anvendelse af NemLog-in*, §3.2.1, published on NemLog-in's [documentation and guides](https://www.nemlog-in.dk/om-nemlog-in/dokumentation-og-vejledninger/) page) set two limits on the local session a login creates:

- **Inactivity:** the session must end after the user has been inactive, with sliding or fixed expiry. The timeout may be **at most 50 minutes**; DIGST **recommends 30 minutes**.
- **Total length:** **at most 8 hours**, after which the user must log in at NemLog-in again. A longer session is only allowed when the user is active throughout and there is a concrete business need that can't reasonably be met within 8 hours.

The library enforces both on the authentication cookie, with the limits from [`session`](configuration.md#session):

- **Inactivity:** `session:idleTimeout`, 30 minutes by default, becomes the cookie's sliding expiry, so each request restarts it. A value over 50 minutes stops the application at startup.
- **Total length:** the sign-in time is stored in the cookie. Once `session:maximumLength`, 8 hours by default, has passed, the next request signs the user out, however active they have been.

Don't set `ExpireTimeSpan` on the cookie scheme yourself; it would override `session:idleTimeout`.

When the local session times out, the next protected page sends the user to `/auth/login`. If the user still has a session at NemLog-in, the login completes without the user entering credentials (single sign-on). A timeout must not send a `LogoutRequest` to the IdP ([OIO-SP-29]), and the library doesn't. An IdP-initiated logout that arrives after the local session timed out is still answered with `Success`.

The `IdleTimeout` of `AddSession` is unrelated to the login. The session only holds pending request IDs and RelayState for the few minutes a login or logout takes, so 30 minutes is ample.

## Middleware

`UseSession` must run before the `/auth` endpoints are reached, and authentication before authorization:

```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();          // required
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();      // or MapControllerRoute(...); the /auth endpoints are attribute-routed
```

Behind a reverse proxy or load balancer, add `app.UseForwardedHeaders(...)` first, so that `HttpContext.Connection.RemoteIpAddress` is the client's address. The library logs it in every audit event (see [Audit logging](#audit-logging)).

When TLS ends at the proxy, the forwarded `X-Forwarded-Proto` header matters too: without it the application sees plain HTTP, so `UseHttpsRedirection` can loop and the redirect to `/auth/login` gets the wrong scheme. The [example in Logging](logging.md#the-client-ip-address) forwards both headers. The assertion consumer and logout URLs aren't affected, as they come from [configuration](configuration.md#serviceprovider).

## Endpoints

`AuthController` provides these routes. They're case-insensitive and allow anonymous access.

| Route                            | Method          | Called by                               | Action                                                                                                         |
|----------------------------------|-----------------|-----------------------------------------|----------------------------------------------------------------------------------------------------------------|
| `/auth/login`                    | `GET`           | your application, or the cookie handler | Builds and signs an `AuthnRequest` and redirects to the IdP. Takes `ReturnUrl`, `RelayState` and `appSwitchPlatform`; see [logging in](#logging-in) |
| `/auth/AssertionConsumerService` | `POST`          | the IdP                                 | Validates the response, signs the user in, and redirects to the return URL or `loginRedirectUrl`               |
| `/auth/logout`                   | `POST`          | your application                        | With no SAML parameters and a valid antiforgery token: initiates logout from the SP                            |
| `/auth/logout`                   | `GET` or `POST` | the IdP                                 | With `SAMLRequest`: IdP-initiated logout. With `SAMLResponse`: the IdP's response to SP-initiated logout       |

The assertion consumer and logout URLs the IdP calls must match your SP metadata exactly. By default they're `{server}/auth/AssertionConsumerService` and `{server}/auth/logout` ([configuration](configuration.md#serviceprovider)).

## Logging in

Send the user to `/auth/login`, for example with a link or a redirect. The cookie handler does this for you when an `[Authorize]` page is opened without a session, adding `ReturnUrl`.

### Configuring what a login requires

What a login requires comes from the configuration, and needs no code:

| Requirement | Configuration |
|---|---|
| Level of assurance | `levelOfAssurance:minimumLoa` (and the NSIS/eIDAS overrides). It is requested in every `AuthnRequest` and checked as a floor on every response |
| OIOSAML 3 profile | `login:profile`: `Person` or `Professional` |
| OIOSAML 4 attribute profiles | `login:attributeProfiles` |
| Re-authentication, no interaction | `identityProvider:forceAuthn`, `identityProvider:isPassive` |

See [levelOfAssurance](configuration.md#levelofassurance) and [login](configuration.md#login). `/auth/login` takes only these from the browser:

| Parameter           | Values                          | Effect                                                                                                                                                                   |
|---------------------|---------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ReturnUrl`         | A local path, e.g. `/orders/42` | Where to send the user after login. Ignored unless it's a local URL                                                                                                      |
| `RelayState`        | Any text                        | Application state. It stays in the session and is handed back after login (see below); only a 22-character token goes to the IdP, so the 80-byte SAML limit doesn't apply |
| `appSwitchPlatform` | `Android`, `iOS`                | Adds the AppSwitch extension, with the return URL configured for that platform in `appSwitchReturnUrls`. An unknown platform is refused                                   |

The library only authenticates. **Check the claims** a page depends on, such as the level of assurance and the identity type, before granting access ([security](security.md#your-applications-responsibilities)).

### Requiring more for one login

If one part of the service needs more than the configuration asks for, for example `High` before a payment, register an [`IAuthnRequestProcessor`](#extension-points). It runs on every `AuthnRequest` just before it's signed, and the response is checked against the request as the processor left it. Take the requirement from server-side state that your own code set, never from the query string:

```csharp
public class StepUpProcessor(IHttpContextAccessor httpContextAccessor) : IAuthnRequestProcessor
{
    public const string RequiredLoaKey = "RequiredLoa";

    public void Process(Saml20AuthnRequest request)
    {
        var session = httpContextAccessor.HttpContext?.Session;
        if (session?.GetString(RequiredLoaKey) is not { } loa) return;
        session.Remove(RequiredLoaKey);
        request.Request.RequestedAuthnContext = new RequestedAuthnContext
        {
            Comparison = AuthnContextComparisonType.minimum,
            ComparisonSpecified = true,
            ItemsElementName = [ItemsChoiceType7.AuthnContextClassRef],
            Items = [OioSaml4Constants.GenericLoAPrefix + "/" + loa],
        };
    }
}
```

```csharp
builder.Services.AddScoped<IAuthnRequestProcessor, StepUpProcessor>();

// In the controller of the page that needs High, when the user's LoA claim is lower:
HttpContext.Session.SetString(StepUpProcessor.RequiredLoaKey, LoAValues.High);
return Redirect("/auth/login?ReturnUrl=/payments");
```

- **Only raise it.** The configured minimum stays a floor: a request for less is still checked against the minimum.
- **Replacing `RequestedAuthnContext` drops what the configuration put there**, such as `login:profile`. Add it back if you use it, and use `OioSaml4Constants.NsisLoAPrefix` when `levelOfAssurance:requestGenericLoa` is `false`.

### Login parameters for test SPs

A test SP, like the [sample SP](sample.md), can let the user choose what a login requires. With `login:allowParametersFromRequest` set to `true`, `/auth/login` also takes these query parameters, and they override the configuration for that login:

| Parameter           | Values                                                                                          | Effect                                                                                                                                                       |
|---------------------|-------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `levelOfAssurance`  | `Low`, `Substantial`, `High`                                                                    | Asks for at least this level. A level below the configured minimum is refused before anything is sent to the IdP                                             |
| `attributeProfiles` | One or more OIOSAML 4 profiles, e.g. `https://data.gov.dk/eid/Person/DK` with `https://data.gov.dk/eid/Person/DK/Anonymous` (repeat the parameter; see [`attributeProfiles`](configuration.md#login)) | Asks for these attribute profiles instead of `login:attributeProfiles`. The response must carry one of them ([OIO-SP-07])                                    |
| `profile`           | `Person`, `Professional`                                                                        | OIOSAML 3: asks for a person or professional identity instead of `login:profile`. The NameID in the response must be of that kind                            |
| `forceAuthn`        | `true`, `false`                                                                                 | Overrides `identityProvider:forceAuthn`. With `true`, the IdP must authenticate again, and the response's `AuthnInstant` must be newer than the request ([OIO-SP-15]) |
| `isPassive`         | `true`, `false`                                                                                 | Overrides `identityProvider:isPassive`. With `true`, the IdP must not interact with the user                                                                 |

> **Not for production.** The user controls these parameters. Without `allowParametersFromRequest` they are ignored.

An unknown level, profile or attribute profile is refused before anything is sent to the IdP. The full list of attribute profiles is in [`OioSaml4AttributeProfiles`](../src/DK.Gov.Oio.Saml/Constants/OioSaml4AttributeProfiles.cs). The sample's [login form](../samples/DK.Gov.Oio.Saml.TestServiceProvider/Views/Home/Index.cshtml) sends every parameter.

### After login

The user is redirected to `ReturnUrl`, if it was local, or otherwise to `serviceProvider:loginRedirectUrl`. If the login carried `RelayState`, it's appended as `?RelayState=...`.

The library never sends your RelayState to the IdP. It sends a random token, keeps your value in the session, and only hands your value back when the IdP returns the exact token. The redirect target never comes from RelayState.

A response that fails validation is rejected and user is not logged in. See [Errors](#errors).

## The signed-in user

`User.Identity.Name` is the assertion's NameID, for example `https://data.gov.dk/model/core/eid/person/uuid/…`. Every SAML attribute in the assertion becomes a claim whose type is the attribute name, such as `https://data.gov.dk/model/core/eid/cprNumber` or `https://data.gov.dk/concept/core/nsis/loa`. The library also adds:

| Claim type (`ClaimsConstants`)         | Value                                                                                        |
|----------------------------------------|----------------------------------------------------------------------------------------------|
| `SubjectNameId`, `SubjectNameIdFormat` | The NameID and its format, used for logout                                                   |
| `SessionIndex`                         | The IdP's session index, used for logout                                                     |
| `Issuer`                               | The IdP's entity ID                                                                          |
| `LocalSessionId`                       | A random ID for this login, written in the audit log so log lines can be tied to the session |

```csharp
var cpr = User.FindFirst("https://data.gov.dk/model/core/eid/cprNumber")?.Value;
```

Which attributes arrive depends on the attribute profile and your declaration of requested attributes in the SP metadata registered at NemLog-in Administration.

## Logging out

**From your application:** post a form to `/auth/logout` without SAML parameters. The form must carry ASP.NET Core's antiforgery token, so another site can't log the user out; a request without a valid token gets `400 Bad Request`, and a `GET` gets `405 Method Not Allowed`. A form built with tag helpers adds the token by itself, as in the sample's [layout](../samples/DK.Gov.Oio.Saml.TestServiceProvider/Views/Shared/_Layout.cshtml):

```cshtml
<form asp-controller="Auth" asp-action="Logout" method="post">
    <button type="submit">Log out</button>
</form>
```

Without Razor views, for example from a single-page application, get a token from `IAntiforgery.GetAndStoreTokens(HttpContext)` and send it as the `__RequestVerificationToken` form field. An optional `RelayState` field is kept in the session and handed back afterwards, as with login.
1. The local session ends first ([OIO-SP-28]).
2. The library sends a signed `LogoutRequest` to the IdP's single logout service.
3. When the IdP answers, the user is redirected to `serviceProvider:logoutRedirectUrl`.

With [`identityProvider:oneTimeUse`](configuration.md#one-time-use), NemLog-in keeps no session for the service, so end the session locally with `HttpContext.SignOutAsync(Saml20Constants.AuthenticationScheme)` instead.

A user without the SAML NameID and session index claims, for example one signed in by another scheme, is refused rather than sent to the IdP with an incomplete request.

**From the IdP:** when the user logs out at another service, NemLog-in sends a signed `LogoutRequest` to `/auth/logout`. The library checks that
- the request is signed by the IdP,
- it names the signed-in user and
- it was sent to the right address.

If all checks pass, it terminates the local session and answers `Success`. A request for a user who isn't signed in to your service is also answered `Success`, as there is no session left to end. If a check fails, it answers `RequestDenied` and keeps the session. A request from anyone other than the configured, trusted IdP receives no answer at all.

## Registering the service provider at NemLog-in

The library doesn't publish SP metadata. You write the metadata file and upload it in the NemLog-in administration portal, the sample's [OIOSAML 3](../samples/DK.Gov.Oio.Saml.TestServiceProvider/Metadata/sp-oiosaml3-metadata.xml) and [OIOSAML 4](../samples/DK.Gov.Oio.Saml.TestServiceProvider/Metadata/sp-oiosaml4-metadata.xml) files are templates. It must contain:

- **Entity ID:** `serviceProvider:entityId`.
- **Certificate:** the SP certificate, for signing and encryption. NemLog-in holds only one certificate per SP; see [replacing the service provider certificate](certificates.md#replacing-the-service-provider-certificate).
- **`AssertionConsumerService`:** HTTP-POST binding at `{server}/auth/AssertionConsumerService`.
- **`SingleLogoutService`:** HTTP-Redirect and/or HTTP-POST at `{server}/auth/logout`.
- **Requested attributes:** the attributes and attribute profiles your service needs.

The IdP side goes the other way: download the IdP's metadata from the portal and point `identityProvider:metadataFile` at it.

Note that this library only supports the recommended encryption ciphers, which are used by default, so you should not declare other encryption (AES-CBC) ciphers in your metadata.

## Running more than one instance

Two pieces of state must be shared when the application runs on several servers:

- **The session** holds the pending `AuthnRequest` and `LogoutRequest` IDs and RelayState values. The response can reach a different instance than the request, so back the session with a shared `IDistributedCache` (Redis, SQL Server, …) instead of `AddDistributedMemoryCache`. Sticky sessions also work.
- **The replay cache** stores accepted assertion IDs, so an assertion can't be used twice. `AddOioSaml` uses the registered `IDistributedCache` when there is one, so a shared cache covers both. With no such cache present the library falls back to an in-memory cache, which protects one instance only. `IDistributedCache` has no atomic "add if absent", so the shared replay cache checks and then stores: two copies of one assertion arriving at two instances within the same moment could both be accepted. If that matters, implement `IAssertionReplayCache` with an atomic insert, for example Redis `SET key value NX EX seconds`, and register it after `AddOioSaml`.

ASP.NET Core data protection keys must also be shared, as for any cookie-authenticated application across servers.

## Errors

The library throws exceptions when something is wrong. It doesn't turn errors into pages. Handle them with ASP.NET Core's exception handling, e.g. `app.UseExceptionHandler("/error")`.

| Exception                      | When                                                                                                                                                                                                      |
|--------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Saml20ValidationException`    | A response or logout message failed validation (signature, audience, LoA, replay, RelayState, …). The message names the rule, often with its OIOSAML requirement id                                       |
| `Saml20StatusException`        | The IdP answered a login or an SP-initiated logout with a status other than `Success`, for example `NoPassive`. A subclass of `Saml20ValidationException`; see below                                      |
| `Saml20FormatException`        | A message is malformed: not base64-encoded XML, inflating beyond the size limit, containing a DTD, or encrypted in a way the library doesn't support                                                     |
| `Saml20Exception`              | A request couldn't be built or handled, e.g. an unknown login parameter or AppSwitch platform without a configured return URL, no single logout service in the IdP metadata, no signed-in user to log out |
| `ConfigurationErrorsException` | Invalid configuration at startup                                                                                                                                                                          |

Every failed login and logout is also written to the audit log with its reason.

`Saml20StatusException` carries the IdP's answer, so you can show the user something more helpful than a general error page. NemLog-in doesn't send an error status when the user cancels the login; the user simply doesn't return.

| Property        | Content                                                                                    |
|-----------------|--------------------------------------------------------------------------------------------|
| `StatusCode`    | The top-level status, e.g. `urn:oasis:names:tc:SAML:2.0:status:Responder`                 |
| `SubStatusCode` | The second-level status, if any, e.g. `…:status:AuthnFailed` or `…:status:NoPassive`       |
| `StatusMessage` | The IdP's message, if any                                                                  |

```csharp
public IActionResult Error()
{
    var error = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
    return error is Saml20StatusException status
        ? View("LoginNotCompleted", status.SubStatusCode)
        : View("Error");
}
```

A login response with an error status carries no signed assertion, so use the status only to choose what to show the user. `NoPassive` is the expected answer to a login with `isPassive` when the user has no session at NemLog-in, and always when `forceAuthn` is also set, as authenticating again requires the user.

## Audit logging

Login and logout events are logged through `Microsoft.Extensions.Logging` under one category, `DK.Gov.Oio.Saml.Audit`. They're structured with separate fields (request and response IDs, NameID, LoA, local session ID, …). The events follow appendix A of NemLog-in's logging policy for service providers (*Logningspolitik for tjenesteudbydere tilsluttet NemLog-in*), which is published on NemLog-in's [documentation and guides](https://www.nemlog-in.dk/om-nemlog-in/dokumentation-og-vejledninger/) page. The `/auth` endpoints add a `ClientIp` scope to every event.

Route the category to storage that keeps logs for at least six months. [Logging](logging.md) lists every event and its fields, maps them to the policy, and lists what the policy requires of you beyond the library: the internal user account, retention, protection of the logs and time synchronisation.

## Extension points

`AddOioSaml` registers default implementations. Register your own after calling it to replace or extend them.

| Interface                          | Default                                                                  | Use it to                                                                                                                                                  |
|------------------------------------|--------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IAuthnRequestProcessor`           | `NoopAuthnRequestProcessor`                                              | Change each `AuthnRequest` just before it's signed. Every registered processor runs. `Saml20AuthnRequest.Endpoint` and `EndpointMetadata` describe the IdP |
| `ICertificateValidator`            | `RevocationCertificateValidator`                                         | Decide whether an IdP signing certificate is acceptable (validity period, then OCSP, then CRL)                                                             |
| `IAssertionReplayCache`            | `DistributedCacheAssertionReplayCache` or `InMemoryAssertionReplayCache` | Store accepted assertion IDs elsewhere                                                                                                                     |
| `IIdentityProviderMetadataService` | `IdentityProviderMetadataService`                                        | Obtain IdP metadata other than from the configured file                                                                                                    |

```csharp
builder.Services.AddOioSaml(builder.Configuration.GetSection("oiosaml"));
builder.Services.AddScoped<IAuthnRequestProcessor, MyAuthnRequestProcessor>();
```
