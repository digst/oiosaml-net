# DK.Gov.Oio.Saml

A .NET SAML 2.0 Service Provider for [NemLog-in](https://www.nemlog-in.dk/), the Danish national identity provider. It implements the SP side of the OIOSAML Web SSO Profile 4.0.1, and OIOSAML 3 through configuration. Both are published on DIGST's [OIOSAML profiles](https://digst.dk/it-loesninger/standarder/oiosaml-profiler/) page.

The library covers:

- **Login:** it builds and signs the `AuthnRequest`, then validates the signed and encrypted assertion that comes back.
- **Logout:** it handles single logout started by the SP and by the IdP.
- **Audit logging:** it writes the events the NemLog-in logging policy requires.

It replaces the legacy `dk.nita.saml20` packages from [OIOSAML.Net](https://github.com/digst/OIOSAML.Net) and follows the [OIOSAML.Java](https://github.com/digst/OIOSAML.Java) reference implementation. Unlike the legacy library it runs on Linux and macOS as well as Windows.

> **Status:** release candidate. The packages are on nuget.org: [`DK.Gov.Oio.Saml.AspNetCore`](https://www.nuget.org/packages/DK.Gov.Oio.Saml.AspNetCore), [`DK.Gov.Oio.Saml.AspNet`](https://www.nuget.org/packages/DK.Gov.Oio.Saml.AspNet) and [`DK.Gov.Oio.Saml`](https://www.nuget.org/packages/DK.Gov.Oio.Saml).

## What it supports

| Feature       | Description                                                                                                                                                                                         |
|---------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Profiles      | OIOSAML 4.0.1, plus OIOSAML 3 (selected by configuration)                                                                                                                                           |
| Bindings      | HTTP-Redirect for requests; HTTP-POST for the response; HTTP-Redirect and HTTP-POST for logout                                                                                                      |
| Login options | Configured, with the minimum level of assurance as a floor: level of assurance, OIOSAML 4 attribute profiles, OIOSAML 3 person/professional profile, `ForceAuthn`, `IsPassive`; AppSwitch      |
| Security      | Mandatory assertion signature, assertions must be encrypted (AES-GCM), algorithm allow-list, replay protection, RelayState bound to the request, IdP certificate revocation checking (OCSP and CRL) |
| Certificates  | SP key from a PKCS#12 file, PEM files, the Windows certificate store, or code; expiry is checked                                                                                                    |
| Hosting       | ASP.NET Core on .NET 10, and ASP.NET MVC 5 with OWIN on .NET Framework 4.7.2 or later. The core library targets `netstandard2.0`                                                                    |

## Projects

| Project                                                                                                    | Target           | Purpose                                                                                       |
|------------------------------------------------------------------------------------------------------------|------------------|-----------------------------------------------------------------------------------------------|
| [`src/DK.Gov.Oio.Saml`](src/DK.Gov.Oio.Saml)                                                               | `netstandard2.0` | Core SAML logic, independent of any host                                                      |
| [`src/DK.Gov.Oio.Saml.AspNetCore`](src/DK.Gov.Oio.Saml.AspNetCore)                                         | `net10.0`        | ASP.NET Core integration: the `/auth` endpoints, DI registration, cookie sign-in              |
| [`src/DK.Gov.Oio.Saml.AspNet`](src/DK.Gov.Oio.Saml.AspNet)                                                 | `net472`         | .NET Framework integration: the `/auth` endpoints on MVC 5, OWIN cookie sign-in, DI resolver  |
| [`samples/DK.Gov.Oio.Saml.TestServiceProvider`](samples/DK.Gov.Oio.Saml.TestServiceProvider)               | `net10.0`        | A runnable SP registered on NemLog-in DevTest4                                                |
| [`samples/DK.Gov.Oio.Saml.AspNet.TestServiceProvider`](samples/DK.Gov.Oio.Saml.AspNet.TestServiceProvider) | `net472`         | The same SP on ASP.NET MVC 5 and IIS Express                                                  |
| [`test/DK.Gov.Oio.Saml.Tests`](test/DK.Gov.Oio.Saml.Tests)                                                 | `net10.0`        | xUnit tests for the core library                                                              |
| [`test/DK.Gov.Oio.Saml.AspNetCore.Tests`](test/DK.Gov.Oio.Saml.AspNetCore.Tests)                           | `net10.0`        | xUnit tests for the ASP.NET Core integration                                                  |
| [`test/DK.Gov.Oio.Saml.AspNet.Tests`](test/DK.Gov.Oio.Saml.AspNet.Tests)                                   | `net472`         | xUnit tests for the .NET Framework integration; they run on Windows only                      |

## Quick start

### Run the sample first

The sample service provider, [`samples/DK.Gov.Oio.Saml.TestServiceProvider`](samples/DK.Gov.Oio.Saml.TestServiceProvider), is the best place to start exploring. It runs out of the box against NemLog-in DevTest4 (inttest-leg).

```bash
dotnet run --project samples/DK.Gov.Oio.Saml.TestServiceProvider     # https://localhost:20002
```

Open https://localhost:20002 to:

- choose the login options 
- log in with a DevTest4 test user, and see the returned claims and RelayState
- log out again

The sample runs in OIOSAML 3 mode by default. Set `AppSettings:OiosamlProfile` to `oiosaml4` for OIOSAML 4. Each mode is registered on DevTest4 as its own SP. Its `Program.cs` is the reference for the wiring in the steps below, and [docs/sample.md](docs/sample.md) describes it in detail.

### Add it to your application

1. Add `DK.Gov.Oio.Saml.AspNetCore`, which brings in the core library: `dotnet add package DK.Gov.Oio.Saml.AspNetCore --prerelease`.
2. Get an OCES organisation certificate for your SP, and download the IdP's metadata from the NemLog-in administration portal.
3. Configure:

   ```json
   "oiosaml": {
     "serviceProvider": {
       "entityId": "https://saml.my-service.example.dk",
       "server": "https://my-service.example.dk",
       "loginRedirectUrl": "/",
       "certificate": { "filePath": "certificates/sp.p12" }
     },
     "identityProvider": {
       "entityId": "https://saml.test-devtest4-nemlog-in.dk",
       "metadataFile": "idp-metadata/devtest4-idp-metadata.xml"
     }
   }
   ```

   Passwords can be omitted from `appsettings.json` by using environment variables, e.g. `oiosaml__serviceProvider__certificate__password`.
4. Register and wire up:

   ```csharp
   // The login cookie is SameSite=None: validate antiforgery tokens on your own state-changing requests.
   builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
   builder.Services
       .AddOioSaml(builder.Configuration.GetSection("oiosaml"))
       .AddOioSamlCookieAuthentication();
   builder.Services.AddHttpContextAccessor();
   builder.Services.AddDistributedMemoryCache();
   builder.Services.AddSession(o =>
   {
       o.Cookie.SameSite = SameSiteMode.None;            // the IdP posts the response cross-site
       o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
       o.Cookie.HttpOnly = true;
       o.Cookie.IsEssential = true;
   });

   var app = builder.Build();
   app.UseHttpsRedirection();
   app.UseRouting();
   app.UseSession();                                    // required: pending request IDs live in the session
   app.UseAuthentication();
   app.UseAuthorization();
   app.MapControllers();
   ```
5. Register your SP's metadata at NemLog-in, with the assertion consumer URL `https://my-service.example.dk/auth/AssertionConsumerService` and the logout URL `https://my-service.example.dk/auth/logout`.
6. Protect pages with `[Authorize]`. Send users to `/auth/login` to log in, and post a form with an antiforgery token to `/auth/logout` to log out.

[docs/integration.md](docs/integration.md) explains each step, and [docs/configuration.md](docs/configuration.md) lists every setting. For an ASP.NET MVC 5 application on .NET Framework, reference `DK.Gov.Oio.Saml.AspNet` instead and follow [docs/integration-aspnet.md](docs/integration-aspnet.md).

## Documentation

- [Integrating with ASP.NET Core](docs/integration.md): registration, session lifetime, CSRF protection, middleware, endpoints, what a login requires, claims, logout, SP metadata, extension points.
- [Integrating with ASP.NET on .NET Framework](docs/integration-aspnet.md): OWIN startup, `Web.config`, the MVC 5 endpoints, cookie customisation, other DI containers.
- [Configuration reference](docs/configuration.md): every option in `OioSamlOptions`, its default, and what is checked at startup.
- [Certificates](docs/certificates.md): loading and replacing the SP certificate, expiry, and how the IdP's certificates are trusted and checked for revocation.
- [Logging](docs/logging.md): the audit events and how they meet NemLog-in's logging policy.
- [Security](docs/security.md): what is validated, what is refused, and what your application must do.
- [The sample service provider](docs/sample.md): running it, what it shows, and adapting it.
- [Migrating from dk.nita.saml20](docs/migration-from-dk.nita.saml20.md): the legacy configuration, endpoints and API mapped to their replacements.
- [Releasing](docs/releasing.md): versions, the release workflow, and the one-time nuget.org and GitHub setup.

## Building and testing

Requires the .NET 10 SDK. The .NET Framework projects build with it on every platform; running the .NET Framework sample needs Windows and IIS Express.

```bash
dotnet build DK.Gov.Oio.Saml.slnx
dotnet test --solution DK.Gov.Oio.Saml.slnx
dotnet test --project test/DK.Gov.Oio.Saml.Tests --filter-class "*LoginFlowTests"   # one test class
```

The tests run on xUnit.net v3 4.x and the Microsoft Testing Platform, selected in `global.json`, so `dotnet test` takes `--solution` or `--project` rather than a path. The filters are xUnit's own (`--filter-class`, `--filter-method`, …); `dotnet test --project <project> -?` lists them.

To measure coverage, as CI does:

```bash
dotnet test --solution DK.Gov.Oio.Saml.slnx --coverlet --results-directory TestResults
```

Each test project writes a `coverage.cobertura.<timestamp>.xml` to `TestResults`; the filters are in [`test/testconfig.json`](test/testconfig.json). Coverage is measured for the core library and the integration layers, without the generated `Schema` classes.

## Contributing

- Every change goes through a feature branch named after its Jira issue (`feature/<KEY>-<description>`) and a pull request into `main`.
- CI builds and runs the tests on Ubuntu for every push and pull request, runs the .NET Framework tests in a separate Windows job, and shows the coverage in the run summary, with the full HTML report as the `coverage-report` artifact.
- Package versions are managed centrally in [`Directory.Packages.props`](Directory.Packages.props): a project references a package without a `Version`, and adding a package means adding its `PackageVersion` there.
- CI fails on an unused `using` directive; check locally with `dotnet build DK.Gov.Oio.Saml.slnx -p:CheckUnusedUsings=true`.
- CI fails when a package, direct or indirect, has a known high or critical vulnerability; a local build only warns. Dependabot raises alerts and update pull requests for NuGet packages and GitHub Actions.
- Releases are cut by tagging a commit on `main`; the release workflow publishes the NuGet packages. See [releasing](docs/releasing.md).

## License

DK.Gov.Oio.Saml is developed by Digitaliseringsstyrelsen (the Danish Agency for Digital Government) and licensed under the [Mozilla Public License 2.0](LICENSE). It is derived from `dk.nita.saml20` ([OIOSAML.Net](https://github.com/digst/OIOSAML.Net)), which is licensed under MPL 1.1; MPL 1.1 §6.2 allows its code to be used under MPL 2.0.

The sample bundles the [Det Fælles Designsystem](samples/DK.Gov.Oio.Saml.TestServiceProvider/wwwroot/lib/dkfds/LICENSE.md) styles and fonts under their own licences. They are not part of the packages.
