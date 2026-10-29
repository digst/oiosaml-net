using DK.Gov.Oio.Saml.Certificates;
using Microsoft.Extensions.Hosting;

namespace DK.Gov.Oio.Saml.AspNetCore.Services;

public class CertificateMonitoringService(IdentityProviderCertificateRefresh identityProviderRefresh, ServiceProviderCertificateExpiry serviceProviderExpiry) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(identityProviderRefresh.RunAsync(stoppingToken), serviceProviderExpiry.RunAsync(stoppingToken));
}
