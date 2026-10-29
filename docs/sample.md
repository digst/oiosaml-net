# The sample service provider

[`samples/DK.Gov.Oio.Saml.TestServiceProvider`](../samples/DK.Gov.Oio.Saml.TestServiceProvider) is a small ASP.NET Core MVC application that uses the library. It is registered at NemLog-in's DevTest4 environment, so it runs out of the box: you can log in, see what NemLog-in returns, and log out. Its `Program.cs` is also the reference for the [integration guide](integration.md).

The sample is for development and exploration only. Its SP certificate, `certificates/serviceprovider.p12`, is a test certificate included in the source code together with its password, so anyone can use it, never use it for a real service. The sample is registered only at DevTest4.

- [Running it](#running-it)
- [OIOSAML 3 and OIOSAML 4](#oiosaml-3-and-oiosaml-4)
- [What you can try](#what-you-can-try)
- [Files](#files)
- [Logs](#logs)
- [Using it as a starting point](#using-it-as-a-starting-point)
- [The .NET Framework sample](#the-net-framework-sample)

## Running it

You need the .NET 10 SDK. The SP certificate comes with the sample. For HTTPS on localhost, Kestrel uses the ASP.NET Core development certificate, trust it once with `dotnet dev-certs https --trust`.

```bash
dotnet run --project samples/DK.Gov.Oio.Saml.TestServiceProvider
```

Open https://localhost:20002. The address must be exactly this: it is the one in the sample's SP metadata at NemLog-in, and the IdP sends the user back to it.

`dotnet run` uses the `https` launch profile, which sets `ASPNETCORE_ENVIRONMENT=Development`. That environment supplies the certificate password from `appsettings.Development.json` and shows exceptions in the browser. Without it, startup fails because the certificate can't be opened.

## OIOSAML 3 and OIOSAML 4

The sample is registered at DevTest4 as two service providers, one per profile version, each with its own entity ID and metadata. `AppSettings:OiosamlProfile` selects which one it runs as:

| Value                 | SP entity ID                           | Configuration file           |
|-----------------------|----------------------------------------|------------------------------|
| `oiosaml3` (default)  | `https://saml.oiosaml-net.dk/oiosaml3` | `appsettings.oiosaml3.json`  |
| `oiosaml4`            | `https://saml.oiosaml-net.dk/oiosaml4` | `appsettings.oiosaml4.json`  |

```bash
dotnet run --project samples/DK.Gov.Oio.Saml.TestServiceProvider -- --AppSettings:OiosamlProfile=oiosaml4
```

`Program.cs` layers the selected file on top of `appsettings.json`. It sets the SP entity ID, the IdP metadata file and `levelOfAssurance:requestGenericLoa`, the setting that decides whether a level of assurance is requested the OIOSAML 4 or the OIOSAML 3 way.

## What you can try

**The front page** is a login form. Every field is a query parameter on `/auth/login`. The sample sets `login:allowParametersFromRequest`, and `levelOfAssurance:minimumLoa` to `Low`, so that each one takes effect ([login parameters for test SPs](integration.md#login-parameters-for-test-sps)):

- the level of assurance, as a generic LoA in OIOSAML 4 mode and as an NSIS LoA in OIOSAML 3 mode,
- OIOSAML 4 attribute profiles, or the OIOSAML 3 person or professional profile,
- the AppSwitch platform. The return URLs in `appsettings.json` are placeholders, so this only shows that the extension is added to the request
- RelayState,
- `ForceAuthn` and `IsPassive`.

Choose "Log in" and log in at NemLog-in with a MitID test identity.

> **Don't copy the login form or `allowParametersFromRequest` into a production SP.** Letting the browser choose the level of assurance, the profiles, `ForceAuthn` and `IsPassive` is useful for testing; a production SP configures them. See [configuring what a login requires](integration.md#configuring-what-a-login-requires).

**The secure page** (`/home/secure`, protected by `[Authorize]`) shows the RelayState you entered and every claim in the login: the NameID, the SAML attributes, the level of assurance, and the claims the library adds (see [the signed-in user](integration.md#the-signed-in-user)). Opening it without a session starts a login by itself.

**Log out** in the header posts the logout form, with its antiforgery token, to `/auth/logout`. The user is logged out locally and at NemLog-in, and returns to `/home`.

## Files

| Path                                   | Content                                                                                                                       |
|----------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|
| `Program.cs`                           | The registration and middleware: `AddOioSaml`, `AddOioSamlCookieAuthentication`, the session, `UseSession`, authentication, and the global antiforgery filter ([why](integration.md#csrf-protection-for-your-own-endpoints)) |
| `appsettings.json`                     | The shared `oiosaml` section: server, redirect URLs, certificate file, IdP entity ID, session lifetime, clock skew              |
| `appsettings.Development.json`         | The certificate password. Acceptable only because the certificate is a test certificate included in the source code              |
| `appsettings.oiosaml3.json`, `…4.json` | What differs between the two registrations                                                                                      |
| `certificates/serviceprovider.p12`     | The SP certificate in the sample's metadata: a test certificate issued to Digitaliseringsstyrelsen, included in the source code |
| `idp-metadata/`                        | DevTest4's IdP metadata, one file per profile version                                                                           |
| `Metadata/`                            | The sample's SP metadata as registered at DevTest4, usable as templates for your own                                            |
| `Controllers/HomeController.cs`        | The front page and the `[Authorize]` secure page. The `/auth` endpoints come from the library                                 |
| `Views/Shared/_Layout.cshtml`          | The logout form                                                                                                               |

## Logs

The console logger writes JSON with scopes, so each event shows its fields and the `ClientIp` scope. Audit events have the category `DK.Gov.Oio.Saml.Audit` and a login produces event 1 when the `AuthnRequest` is sent, then event 2 when the login is accepted or event 3, with the reason, when it is rejected. [Logging](logging.md) lists every event.

## Using it as a starting point

To run the sample as your own SP, replace everything that identifies it:

1. **SP entity ID and server** in `appsettings.json` and the profile file.
2. **Certificate:** your own, with the password outside `appsettings.json` (see [paths and secrets](configuration.md#paths-and-secrets)).
3. **SP metadata:** write it from the templates in `Metadata/` with your entity ID, certificate and URLs, and upload it in NemLog-in Administration (see [registering the service provider](integration.md#registering-the-service-provider-at-nemlog-in)).
4. **IdP metadata:** the file for the environment you connect to, downloaded from NemLog-in Administration.

Drop what you don't need: the profile file switching, runtime Razor compilation and the custom static-file content types are only there for the sample.

## The .NET Framework sample

[`samples/DK.Gov.Oio.Saml.AspNet.TestServiceProvider`](../samples/DK.Gov.Oio.Saml.AspNet.TestServiceProvider) is the same service provider on ASP.NET MVC 5 and .NET Framework 4.7.2, using `DK.Gov.Oio.Saml.AspNet`. It has the same pages, uses the same DevTest4 registrations at the same address, and is the reference for the [.NET Framework integration guide](integration-aspnet.md). It runs on Windows only.

It shares the certificate, the IdP metadata and the stylesheet with the ASP.NET Core sample: the build copies `certificates/`, `idp-metadata/` and the `dkfds` stylesheet from there into the project folder, which IIS serves. Run one sample at a time, as both listen on https://localhost:20002.

**Running it** in Visual Studio: open the solution, set the project as the startup project and press F5. It starts in IIS Express with the `IIS Express` launch profile. From a terminal, build it and point IIS Express at the project folder with an HTTPS binding on port 20002:

```powershell
dotnet build samples/DK.Gov.Oio.Saml.AspNet.TestServiceProvider
$config = "$env:TEMP\oiosaml-applicationhost.config"
Copy-Item "$env:ProgramFiles\IIS Express\config\templates\PersonalWebServer\applicationhost.config" $config
& "$env:ProgramFiles\IIS Express\appcmd.exe" add site /name:OioSaml /physicalPath:"$PWD\samples\DK.Gov.Oio.Saml.AspNet.TestServiceProvider" /bindings:"https/*:20002:localhost" /apphostconfig:$config
& "$env:ProgramFiles\IIS Express\iisexpress.exe" /config:$config /site:OioSaml
```

IIS Express serves HTTPS on a port only when HTTP.sys has a certificate bound to it. Its installer binds ports 44300 to 44399; for port 20002, bind the IIS Express development certificate once from an elevated prompt with `netsh http add sslcert ipport=0.0.0.0:20002 certhash=<thumbprint> appid={214124cd-d05b-4309-9af9-9caa44b2b74a}`, using the thumbprint of the "IIS Express Development Certificate" in the local machine store.

**OIOSAML 3 or 4** is selected by the `OiosamlProfile` app setting in `Web.config`, `oiosaml4` by default.

| Path                            | Content                                                                                                                       |
|---------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| `Startup.cs`                    | The OWIN startup: `AddOioSaml` in code, the dependency resolver, attribute routes and `UseOioSaml`                            |
| `Web.config`                    | Session and cookie settings, query string limits, the `netstandard` reference for views, and the generated binding redirects  |
| `Controllers/HomeController.cs` | The front page and the `[Authorize]` secure page                                                                              |
| `Views/Shared/_Layout.cshtml`   | The logout form, with `Html.AntiForgeryToken()`                                                                               |

The audit log goes to the debugger output and, as JSON, to the IIS Express console.
