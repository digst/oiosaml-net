# Certificates

The library handles two types of certificates:

- **The service provider's own certificate**: yours, along with its private key. It signs `AuthnRequest`s and logout messages, and the IdP encrypts assertions to it.
- **The identity provider's signing certificates**: NemLog-in's, from its metadata. They verify the signatures on assertions and logout messages.

The related settings are described in the [configuration reference](configuration.md#service-provider-certificate), and this page covers obtaining, loading, monitoring and replacing/updating certificates.

- [The service provider certificate](#the-service-provider-certificate)
- [Expiry monitoring](#expiry-monitoring)
- [Replacing the service provider certificate](#replacing-the-service-provider-certificate)
- [The identity provider's certificates](#the-identity-providers-certificates)
- [Revocation checking](#revocation-checking)
- [Replacing the identity provider's certificates](#replacing-the-identity-providers-certificates)

## The service provider certificate

Use the certificate registered in your SP metadata at NemLog-in, usually an OCES organisation or system certificate obtained from MitID Erhverv.
The associated private key must be available to the process.

Choose where to load it from:

| Source                    | Suits                                                                      | Notes                                                                                                                                                   |
|---------------------------|----------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| PKCS#12 file (`.p12`)     | Most deployments                                                           | Works on Windows, Linux and macOS. The password goes in an environment variable or secret store, never in `appsettings.json`                            |
| PEM files                 | Containers and Kubernetes, where the certificate and key are mounted files | Certificate and key in one file or two (`keyFilePath`). PKCS#1 or PKCS#8 RSA key, plain or encrypted                                                    |
| Windows certificate store | Windows servers where certificates are managed centrally                   | `thumbprint` and `storeLocation`. Grant the application pool or service account read access to the private key (MMC: *All Tasks › Manage Private Keys*) |
| Code                      | Azure Key Vault, HSM-backed or other custom sources                        | `options.ServiceProvider.SetCertificate(certificate)`, which takes precedence over configuration. The certificate must carry its private key            |

On Linux and Windows the key from a file is held in memory only. On macOS it goes into a temporary keychain, as the OS requires.

Every problem loading the certificate stops the application at startup, with a message naming the file or thumbprint: a wrong password, a missing key, a key that doesn't belong to the certificate, an expired certificate, or a thumbprint not found.

## Expiry monitoring

- **At startup:** an expired SP certificate stops the application.
- **While running:** a background service checks once a day. Within 30 days of expiry it logs a **warning**. Once a certificate has expired it logs an **error**. The category is `DK.Gov.Oio.Saml.Certificates.ServiceProviderCertificateExpiry`. Monitor these events in the log.

After expiry NemLog-in rejects the SP's signatures, so logins stop working.

## Replacing the service provider certificate

NemLog-in holds one certificate per SP, so the SP can use only one certificate at any given time, i.e. `serviceProvider:certificate`. When NemLog-in switches to new SP metadata, it encrypts assertions to the new certificate and only accepts signatures made with it, so the SP must switch at the same moment.

NemLog-in Administration lets you upload new metadata with an activation date and time. Use it to plan the switch:

1. **Get the new certificate**, and prepare the configuration that uses it.
2. **Upload new SP metadata** with the new certificate in NemLog-in Administration, and set its activation time, for example at a quiet hour.
3. **At the activation time, deploy the new certificate.**

Between activation and deployment, logins fail since NemLog-in rejects `AuthnRequest`s and logout messages signed with the old certificate.

## The identity provider's certificates

The IdP's signing certificates come from the `KeyDescriptor`s in its metadata file (`identityProvider:metadataFile`), those marked `use="signing"` or without `use`. Trust rests on that file since the library doesn't build a chain to a root CA. Download the metadata from NemLog-in over a trusted channel and protect the file from modification.

A signature verifies only with one of these certificates, and only while the certificate is acceptable:

- it is within its validity period, allowing for `allowedClockSkew`;
- it isn't revoked, when revocation checking is enabled (the default).

When none of the IdP's certificates is acceptable, every login fails with "Identity provider '…' has no valid signing certificate", followed by the reason for each certificate.

Currently, NemLog-in publishes only metadata containing a single signing certificate at a time. Note, however, that this library
supports more than one signing certificate in NemLog-in IdP metadata and will attempt signature validation with any listed certificate.

You may take advantage of this by including an upcoming NemLog-in IdP signing certificate in the metadata file as soon as NemLog-in releases that certificate, and deploy that file.
In this case you are not forced to update the NemLog-in IdP metadata at the same time as NemLog-in begins using the new certificate,
since the library will just verify the signatures with the new certificate, already available.

## Revocation checking

The validator, `RevocationCertificateValidator`, checks each IdP certificate:

1. **Validity period.**
2. **OCSP:** a signed request with a nonce, sent by POST to each responder named in the certificate's Authority Information Access extension. The response must be signed by the issuing CA or a responder it authorised, be current, and cover the certificate. A response that neither echoes the nonce nor gives a `nextUpdate` is believed for one day from its `thisUpdate`, as it could otherwise be an old answer replayed.
3. **CRL:** when OCSP gives no answer, the CRLs named in the certificate's distribution points. A CRL must be signed by the issuing CA and be current.

The issuing CA certificate is fetched from the certificate's `caIssuers` location and accepted only if it really issued the certificate. Only `http` and `https` locations are used.

**Network access:** the server must reach the OCSP responders, CRL distribution points and CA certificate locations named in NemLog-in's certificates. Open the firewall for them; show them with `openssl x509 -in idp.crt -noout -ext authorityInfoAccess,crlDistributionPoints`.

**Timing:** a background service re-checks the certificates every `certificateRevocation:checkInterval` (10 minutes by default). Logins use the latest result and never wait for OCSP or CRL, except for a certificate not seen before, such as after a metadata change.

**When the status can't be determined** (responders unreachable, invalid responses), a certificate that was last seen valid stays accepted for `certificateRevocation:gracePeriod` (4 hours by default). A revoked or expired certificate is dropped at once, and a certificate never seen valid isn't accepted. Someone who can block the server's OCSP and CRL traffic can therefore keep a revoked certificate accepted for at most the grace period, but can't make the library believe a forged "good" answer. Shorten `gracePeriod` if that window is too long for you.

Every problem found by the background check is logged as a **warning** under `DK.Gov.Oio.Saml.Certificates.IdentityProviderCertificateRefresh`, with a note when the certificate is still accepted within the grace period. Alert on it: once the grace period runs out, logins stop.

You may disable revocation checking by setting `certificateRevocation:enabled` to `false`. In that case, a warning is logged at startup.

To apply your own policy for IdP certificate validation, implement and register an `ICertificateValidator` after `AddOioSaml` (see [extension points](integration.md#extension-points)).

## Replacing the identity provider's certificates

NemLog-in announces a change of its signing certificate in advance and publishes new metadata. Replace your copy before the old certificate is retired:

1. Download the new IdP metadata from the NemLog-in administration portal.
2. Replace the file at `identityProvider:metadataFile`.

The file is read on every use, so the change takes effect without a restart. With several instances, replace it on all of them. The new certificate is checked for revocation on first use.
