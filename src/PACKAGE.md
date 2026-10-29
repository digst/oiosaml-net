# DK.Gov.Oio.Saml

A .NET SAML 2.0 Service Provider for [NemLog-in](https://www.nemlog-in.dk/), the Danish national identity provider. It implements the SP side of the OIOSAML Web SSO Profile 4.0.1 and OIOSAML 3. It replaces the legacy `dk.nita.saml20` packages.

| Package                      | Use it for                                                        |
|------------------------------|-------------------------------------------------------------------|
| `DK.Gov.Oio.Saml.AspNetCore` | ASP.NET Core on .NET 10                                           |
| `DK.Gov.Oio.Saml.AspNet`     | ASP.NET MVC 5 with OWIN on .NET Framework 4.7.2 or later          |
| `DK.Gov.Oio.Saml`            | The host-independent core both build on; referenced automatically |

The library builds and signs the `AuthnRequest`, accepts only encrypted, signed assertions from the configured IdP and checks them against the profile, handles single logout started by either side, checks the IdP's certificates for revocation, and writes the audit events NemLog-in's logging policy requires.

- [Integration guide (ASP.NET Core)](https://github.com/digst/oiosaml-net/blob/main/docs/integration.md)
- [Integration guide (.NET Framework)](https://github.com/digst/oiosaml-net/blob/main/docs/integration-aspnet.md)
- [Configuration reference](https://github.com/digst/oiosaml-net/blob/main/docs/configuration.md)
- [Security](https://github.com/digst/oiosaml-net/blob/main/docs/security.md)
- [Migrating from dk.nita.saml20](https://github.com/digst/oiosaml-net/blob/main/docs/migration-from-dk.nita.saml20.md)
- [Release notes](https://github.com/digst/oiosaml-net/releases)

Developed by Digitaliseringsstyrelsen and licensed under the Mozilla Public License 2.0.
