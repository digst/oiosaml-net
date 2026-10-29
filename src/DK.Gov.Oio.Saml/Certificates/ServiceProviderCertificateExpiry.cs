using System;
using System.Threading;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.Extensions.Logging;

namespace DK.Gov.Oio.Saml.Certificates;

// Startup refuses an expired certificate (ServiceProviderOptions); this catches one that expires while the SP runs.
public partial class ServiceProviderCertificateExpiry(OioSamlOptions options, TimeProvider timeProvider, ILogger<ServiceProviderCertificateExpiry> logger)
{
    public static readonly TimeSpan WarningPeriod = TimeSpan.FromDays(30);

    private readonly ILogger _logger = logger;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Check();
            await timeProvider.Delay(TimeSpan.FromDays(1), cancellationToken).ConfigureAwait(false);
        }
    }

    public void Check()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var certificate = options.ServiceProvider.GetCertificate();
        var notAfter = certificate.NotAfter.ToUniversalTime();
        if (notAfter < now)
            Expired(certificate.Thumbprint, certificate.Subject, notAfter);
        else if (notAfter - now < WarningPeriod)
            ExpiresSoon(certificate.Thumbprint, certificate.Subject, notAfter, (int)(notAfter - now).TotalDays);
    }

    [LoggerMessage(1, LogLevel.Error, "Service provider certificate {Thumbprint} ({Subject}) expired {NotAfter:yyyy-MM-dd HH':'mm} UTC; the IdP will reject its signatures")]
    private partial void Expired(string thumbprint, string subject, DateTime notAfter);

    [LoggerMessage(2, LogLevel.Warning, "Service provider certificate {Thumbprint} ({Subject}) expires {NotAfter:yyyy-MM-dd HH':'mm} UTC, in {Days} days; renew it and update the SP metadata at NemLog-in")]
    private partial void ExpiresSoon(string thumbprint, string subject, DateTime notAfter, int days);
}
