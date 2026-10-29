using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Services;

namespace DK.Gov.Oio.Saml.Configuration;

public class OioSamlOptions
{
    public ServiceProviderOptions ServiceProvider { get; set; } = new();
    public IdentityProvider IdentityProvider { get; set; } = new();
    public TimeSpan AllowedClockSkew { get; set; } = TimeSpan.FromMinutes(3);
    public LevelOfAssuranceOptions LevelOfAssurance { get; set; } = new();
    public LoginOptions Login { get; set; } = new();
    public CertificateRevocationOptions CertificateRevocation { get; set; } = new();
    public SessionOptions Session { get; set; } = new();
    public List<AppSwitchReturnUrl> AppSwitchReturnUrls { get; set; } = [];

    public void Validate()
    {
        var problems = new List<string>();
        if (string.IsNullOrEmpty(ServiceProvider.EntityId)) problems.Add("serviceProvider:entityId is required.");
        if (string.IsNullOrEmpty(ServiceProvider.Server) && (ServiceProvider.AssertionConsumerServiceUrl == null || ServiceProvider.SingleLogoutServiceUrl == null))
            problems.Add("serviceProvider:server is required unless both assertionConsumerServiceUrl and singleLogoutServiceUrl are set.");
        if (string.IsNullOrEmpty(IdentityProvider.EntityId)) problems.Add("identityProvider:entityId is required.");
        if (string.IsNullOrEmpty(IdentityProvider.MetadataFile)) problems.Add("identityProvider:metadataFile is required.");
        else if (!File.Exists(IdentityProvider.GetMetadataFile())) problems.Add($"identityProvider:metadataFile '{IdentityProvider.GetMetadataFile()}' does not exist.");
        else
        {
            try { IdentityProviderMetadataService.Read(IdentityProvider); }
            catch (Exception e) when (e is InvalidOperationException or XmlException) { problems.Add(e.Message); }
        }
        if (AllowedClockSkew < TimeSpan.Zero) problems.Add("allowedClockSkew must not be negative.");
        foreach (var loa in new[] { LevelOfAssurance.MinimumLoa, LevelOfAssurance.GetMinimumNsisLoa(), LevelOfAssurance.GetMinimumEidasLoa() }.Distinct().Where(l => LoAValues.GetLoARank(l) == -1))
            problems.Add($"'{loa}' is not a level of assurance; use {string.Join(", ", LoAValues.AllValuesRanked)}.");
        if (Login.Profile != null && !OioSaml3Profiles.All.Contains(Login.Profile))
            problems.Add($"login:profile '{Login.Profile}' is not a profile; use {string.Join(", ", OioSaml3Profiles.All)}.");
        foreach (var profile in Login.AttributeProfiles.Where(p => !OioSaml4AttributeProfiles.AllProfiles.Contains(p)))
            problems.Add($"login:attributeProfiles '{profile}' is not an attribute profile.");
        if (AppSwitchReturnUrls.Any(x => string.IsNullOrEmpty(x.Value))) problems.Add("Every appSwitchReturnUrl needs a value.");
        if (CertificateRevocation.CheckInterval <= TimeSpan.Zero) problems.Add("certificateRevocation:checkInterval must be positive.");
        if (CertificateRevocation.GracePeriod < TimeSpan.Zero) problems.Add("certificateRevocation:gracePeriod must not be negative.");
        if (Session.IdleTimeout <= TimeSpan.Zero || Session.IdleTimeout > SessionOptions.MaximumIdleTimeout)
            problems.Add($"session:idleTimeout must be positive and at most {SessionOptions.MaximumIdleTimeout.TotalMinutes} minutes (NemLog-in Tekniske krav, 3.2.1).");
        if (Session.MaximumLength <= TimeSpan.Zero) problems.Add("session:maximumLength must be positive.");
        try { ServiceProvider.GetCertificate(); }
        catch (ConfigurationErrorsException e) { problems.Add(e.Message); }
        if (problems.Count > 0) throw new ConfigurationErrorsException("Invalid OIOSAML configuration: " + string.Join(" ", problems));
    }
}

public class ServiceProviderOptions
{
    private X509Certificate2? _codeCertificate;
    private X509Certificate2? _certificate;

    public string EntityId { get; set; } = "";
    public string Server { get; set; } = "";
    public string? AssertionConsumerServiceUrl { get; set; }
    public string? SingleLogoutServiceUrl { get; set; }
    public string LoginRedirectUrl { get; set; } = "/";
    public string LogoutRedirectUrl { get; set; } = "/";
    // The SP's own key material: it signs AuthnRequests and logout messages, and decrypts assertions encrypted to it.
    // One only, as NemLog-in holds one certificate per SP.
    public CertificateSource? Certificate { get; set; }

    // The defaults are AuthController's routes as published in the SP metadata; the IdP echoes them and they must match exactly.
    public string GetAssertionConsumerServiceUrl() => AssertionConsumerServiceUrl ?? Server.TrimEnd('/') + "/auth/AssertionConsumerService";
    public string GetSingleLogoutServiceUrl() => SingleLogoutServiceUrl ?? Server.TrimEnd('/') + "/auth/logout";

    // Takes precedence over serviceProvider:certificate.
    public ServiceProviderOptions SetCertificate(X509Certificate2 certificate)
    {
        _codeCertificate = certificate;
        _certificate = null;
        return this;
    }

    public X509Certificate2 GetCertificate() => _certificate ??= Load();

    private X509Certificate2 Load()
    {
        var certificate = _codeCertificate ?? Certificate?.Load()
            ?? throw new ConfigurationErrorsException("No service provider certificate is configured. Configure serviceProvider:certificate or call ServiceProvider.SetCertificate.");
        if (!certificate.HasPrivateKey)
            throw new ConfigurationErrorsException($"Service provider certificate {certificate.Thumbprint} has no private key accessible to the process identity.");
        if (certificate.NotAfter.ToUniversalTime() < DateTime.UtcNow)
            throw new ConfigurationErrorsException($"Service provider certificate {certificate.Thumbprint} ({certificate.Subject}) expired {certificate.NotAfter.ToUniversalTime().ToString("yyyy-MM-dd HH':'mm", CultureInfo.InvariantCulture)} UTC.");
        return certificate;
    }
}

public class LevelOfAssuranceOptions
{
    // How a requested LoA is expressed: the OIOSAML 4 generic class ref, or (false) the OIOSAML 3 NSIS class ref.
    // The response is validated on its NSIS or eIDAS LoA either way [OIO-SP-16].
    public bool RequestGenericLoa { get; set; } = true;
    // The floor for NSIS and eIDAS LoA alike, unless overridden per framework [OIO-SP-16]. A login request can only raise it.
    public string MinimumLoa { get; set; } = LoAValues.Substantial;
    // Asks the IdP for the minimum, so the user authenticates at that level rather than being refused afterwards.
    public bool RequestMinimumLoa { get; set; } = true;
    public string? MinimumNsisLoa { get; set; }
    public string? MinimumEidasLoa { get; set; }

    public string GetMinimumNsisLoa() => string.IsNullOrEmpty(MinimumNsisLoa) ? MinimumLoa : MinimumNsisLoa!;
    public string GetMinimumEidasLoa() => string.IsNullOrEmpty(MinimumEidasLoa) ? MinimumLoa : MinimumEidasLoa!;
}

// What every login asks the IdP for. ForceAuthn and IsPassive are under IdentityProvider.
public class LoginOptions
{
    // OIOSAML 3: OioSaml3Profiles.Person or Professional. The NameID must then be of that kind.
    public string? Profile { get; set; }
    // OIOSAML 4: the response must carry one of them [OIO-SP-07].
    public List<string> AttributeProfiles { get; set; } = [];
    // For test SPs only: /auth/login then also takes the level of assurance, profiles, forceAuthn and isPassive from the request,
    // which the user controls. A requested level below the configured minimum is still refused.
    public bool AllowParametersFromRequest { get; set; }
}

public class CertificateRevocationOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromHours(4);
}

public class SessionOptions
{
    public static readonly TimeSpan MaximumIdleTimeout = TimeSpan.FromMinutes(50);

    // Sliding: every request restarts it. NemLog-in allows at most 50 minutes and recommends 30.
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);
    // Counted from sign-in, however active the user is. NemLog-in allows longer than 8 hours only for a documented business need.
    public TimeSpan MaximumLength { get; set; } = TimeSpan.FromHours(8);

    public bool HasEnded(DateTimeOffset signedIn, DateTimeOffset now) => now - signedIn >= MaximumLength;
}
