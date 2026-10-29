# Integrating with ASP.NET on .NET Framework

This guide shows how to add NemLog-in login and logout to an ASP.NET MVC 5 application on .NET Framework 4.7.2 or later with `DK.Gov.Oio.Saml.AspNet`. The [.NET Framework sample](../samples/DK.Gov.Oio.Saml.AspNet.TestServiceProvider/Startup.cs) is a complete, working setup.

The SAML flow is the same as on ASP.NET Core and runs the same core library. This guide covers what differs on .NET Framework. For everything else it links to the [ASP.NET Core guide](integration.md): [what a login requires](integration.md#configuring-what-a-login-requires), [the signed-in user](integration.md#the-signed-in-user), the [logout semantics](integration.md#logging-out), [SP metadata](integration.md#registering-the-service-provider-at-nemlog-in), [errors](integration.md#errors), [audit logging](integration.md#audit-logging) and the [extension points](integration.md#extension-points).

- [How it fits together](#how-it-fits-together)
- [Packages](#packages)
- [Startup](#startup)
- [Web.config](#webconfig)
- [Endpoints](#endpoints)
- [Logging in and out](#logging-in-and-out)
- [Customising the cookie](#customising-the-cookie)
- [Using another DI container](#using-another-di-container)
- [Running more than one instance](#running-more-than-one-instance)

## How it fits together

| Concern                       | ASP.NET Core                             | .NET Framework                                                                     |
|-------------------------------|------------------------------------------|------------------------------------------------------------------------------------|
| Endpoints                     | `AuthController`, ASP.NET Core MVC       | `AuthController`, ASP.NET MVC 5, attribute-routed under `/auth`                    |
| Authentication cookie         | ASP.NET Core cookie authentication       | OWIN (Katana) cookie authentication on `Microsoft.Owin.Host.SystemWeb`             |
| Pending requests, RelayState  | ASP.NET Core session                     | ASP.NET session state                                                              |
| Services                      | `IServiceCollection`                     | The same `IServiceCollection`, exposed to MVC by `ServiceProviderDependencyResolver` |
| Certificate checks            | Hosted service                           | `HostingEnvironment.QueueBackgroundWorkItem`, started by `UseOioSaml`              |
| Logout CSRF protection        | ASP.NET Core antiforgery                 | MVC 5 `AntiForgery`                                                                |

The session lifetime from [`session`](configuration.md#session) is enforced on the OWIN cookie exactly as on ASP.NET Core: `idleTimeout` is the sliding expiry and `maximumLength` is counted from sign-in ([session lifetime](integration.md#session-lifetime)).

## Packages

Reference `DK.Gov.Oio.Saml.AspNet`. It brings in the core library, ASP.NET MVC 5, the OWIN cookie middleware, `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging`. Add a logging provider for the audit log, for example one that writes to your existing log4net or NLog setup.

The packages target .NET Standard and use newer versions of assemblies that ship with .NET Framework, such as `System.Memory`. Let the build generate binding redirects into `Web.config`, or Visual Studio will offer to add them. An SDK-style web project built with [MSBuild.SDK.SystemWeb](https://github.com/CZEMacLeod/MSBuild.SDK.SystemWeb) does this with `<GeneratedBindingRedirectsAction>Overwrite</GeneratedBindingRedirectsAction>`, as the sample does.

## Startup

Configure everything in an OWIN startup class: build the services, hand them to MVC, map the attribute routes and add the cookie.

```csharp
[assembly: OwinStartup(typeof(Startup))]

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddYourProvider());
        services.AddOioSaml(options =>
        {
            options.ServiceProvider.EntityId = "https://saml.my-service.example.dk";
            options.ServiceProvider.Server = "https://my-service.example.dk";
            options.ServiceProvider.Certificate = new CertificateSource { Thumbprint = "…", StoreLocation = StoreLocation.LocalMachine };
            options.IdentityProvider.EntityId = "https://saml.test-devtest4-nemlog-in.dk";
            options.IdentityProvider.MetadataFile = "idp-metadata/devtest4-idp-metadata.xml";
        });
        services.AddTransient<HomeController>();          // your own controllers that take services
        var provider = services.BuildServiceProvider();

        DependencyResolver.SetResolver(new ServiceProviderDependencyResolver(provider));
        RouteTable.Routes.MapMvcAttributeRoutes();        // required: the /auth endpoints are attribute-routed
        RouteTable.Routes.MapRoute("default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });

        app.UseOioSaml(provider);
    }
}
```

`AddOioSaml` takes the same three forms as on ASP.NET Core: code, an `IConfiguration` section, or an `OioSamlOptions` instance ([registering the services](integration.md#registering-the-services)). It validates the configuration, so a wrong setting stops the application at its first request with every problem listed. Relative paths, such as `metadataFile` and a certificate `filePath`, are relative to the application's root folder, where `Web.config` is.

`UseOioSaml`:

- adds the OWIN cookie authentication scheme `OIOSamlAssertion` (`Saml20Constants.AuthenticationScheme`): login path `/auth/login`, `SameSite=None`, `Secure`, `HttpOnly`, and the session lifetime,
- uses Katana's `SystemWebChunkingCookieManager`, which keeps the cookie from clashing with cookies System.Web sets in the same response, such as the session cookie, and splits it when the claims make it large,
- sets `AntiForgeryConfig.UniqueClaimTypeIdentifier` to the `LocalSessionId` claim, unless you set it yourself, because MVC's antiforgery token needs a claim that identifies the user. The login cookie is `SameSite=None`, so put `[ValidateAntiForgeryToken]` on every action of yours that changes state, and `@Html.AntiForgeryToken()` in its form (see [CSRF protection](integration.md#csrf-protection-for-your-own-endpoints)),
- starts the [IdP certificate revocation checks](certificates.md) and the SP certificate expiry check in the background.

Your own controllers resolve their constructor arguments from the same container, as `HomeController` above. A controller with a parameterless constructor needs no registration.

## Web.config

```xml
<configuration>
  <appSettings>
    <add key="owin:AutomaticAppStartup" value="true" />
  </appSettings>
  <system.web>
    <compilation targetFramework="4.7.2">
      <assemblies>
        <add assembly="netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51" />
      </assemblies>
    </compilation>
    <httpRuntime targetFramework="4.7.2" maxQueryStringLength="8192" />
    <authentication mode="None" />
    <sessionState cookieSameSite="None" />
    <httpCookies httpOnlyCookies="true" requireSSL="true" sameSite="None" />
  </system.web>
  <system.webServer>
    <security>
      <requestFiltering>
        <requestLimits maxQueryString="8192" />
      </requestFiltering>
    </security>
    <modules>
      <remove name="FormsAuthentication" />
    </modules>
  </system.webServer>
</configuration>
```

- **Session state must be on**, as it is by default. It holds the pending `AuthnRequest` and `LogoutRequest` IDs and RelayState values, so the session cookie must be `SameSite=None`: the IdP returns the user with a cross-site `POST`, and a `Lax` cookie would be missing on the assertion consumer service ([why](integration.md#registering-the-services)). `cookieSameSite` needs .NET Framework 4.7.2 or later.
- **Forms authentication must be off.** It would turn the 401 from `[Authorize]` into its own redirect instead of letting the OWIN cookie send the user to `/auth/login`.
- **The query string limits must be raised.** NemLog-in sends logout messages on the HTTP-Redirect binding, and with the signature a request is often longer than the 2048 characters that IIS and ASP.NET allow by default. IIS refuses a longer query string with `404.15` before the application sees it.
- **The `netstandard` reference** lets Razor views compile when they use types from the library, such as `Saml20Constants`. Without it a view fails with `CS0012: The type 'Enum' is defined in an assembly that is not referenced`.
- **Request validation** is no obstacle: the endpoints read `Request.Unvalidated`, so a RelayState containing markup isn't refused.

## Endpoints

`AuthController` provides the same routes as on ASP.NET Core ([endpoints](integration.md#endpoints)): `/auth/login`, `/auth/AssertionConsumerService`, and `/auth/logout` for `GET` and `POST`. They allow anonymous access, also under a global `AuthorizeAttribute`.

The assertion consumer and logout URLs the IdP calls must match your SP metadata exactly. By default they're `{server}/auth/AssertionConsumerService` and `{server}/auth/logout` ([configuration](configuration.md#serviceprovider)).

## Logging in and out

**Login** works as on ASP.NET Core: send the user to `/auth/login` ([what it takes](integration.md#configuring-what-a-login-requires)), or let `[Authorize]` do it, adding `ReturnUrl`.

**Logout** from your application is a form posted to `/auth/logout` with MVC's antiforgery token:

```cshtml
@using (Html.BeginForm("Logout", "Auth", FormMethod.Post))
{
    @Html.AntiForgeryToken()
    <button type="submit">Log out</button>
}
```

A request without a valid token gets `400 Bad Request`, and a `GET` without SAML parameters gets `405 Method Not Allowed`. Logout from the IdP needs nothing from you. [Logging out](integration.md#logging-out) describes both directions.

**The signed-in user** is `User` in a controller or view, a `ClaimsPrincipal` with the claims described in [the signed-in user](integration.md#the-signed-in-user):

```csharp
var cpr = ((ClaimsPrincipal)User).FindFirst("https://data.gov.dk/model/core/eid/cprNumber")?.Value;
```

**Errors** are exceptions, as on ASP.NET Core ([errors](integration.md#errors)). Handle them with `customErrors`, a `HandleErrorAttribute`, or `Application_Error`.

## Customising the cookie

Pass a callback to `UseOioSaml` to change the cookie options, for example its name or domain, or to react to sign-in and sign-out. Set the events on the existing provider rather than replacing it, as it also enforces the session lifetime:

```csharp
app.UseOioSaml(provider, cookie =>
{
    cookie.CookieName = ".MyService.Auth";
    cookie.Provider = new CookieAuthenticationProvider
    {
        OnResponseSignedIn = context => { /* after login */ },
        OnResponseSignOut = context => { /* before the local session ends, also for logout from the IdP */ },
    };
});
```

Don't set `ExpireTimeSpan` or `SlidingExpiration`: they come from `session:idleTimeout`.

## Using another DI container

The library registers its services in an `IServiceCollection`. With Autofac, Unity, Simple Injector or another container, populate it from that collection with the container's `Microsoft.Extensions.DependencyInjection` integration, for example Autofac's `builder.Populate(services)`, and use the container's own MVC dependency resolver. `UseOioSaml` takes any `IServiceProvider`. `AuthController` must be resolvable, with one scope per request.

## Running more than one instance

The same state must be shared as on ASP.NET Core ([running more than one instance](integration.md#running-more-than-one-instance)):

- **The session:** use the ASP.NET `StateServer` or `SQLServer` session state mode, or sticky sessions.
- **The replay cache:** register an `IDistributedCache` in the service collection, for example `AddDistributedSqlServerCache`, and the library uses it for accepted assertion IDs.
- **The cookie:** the OWIN cookie is protected with the machine key, so set the same `<machineKey>` on every instance.
