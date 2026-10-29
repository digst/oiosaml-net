using System;
using System.Threading;
using System.Threading.Tasks;
using DK.Gov.Oio.Saml.Configuration;
using Microsoft.Extensions.Logging;

namespace DK.Gov.Oio.Saml.Certificates;

// Keeps the store's verdicts current so logins never wait on OCSP or CRL; the host runs it in the background.
public class IdentityProviderCertificateRefresh(
    IIdentityProviderCertificateStore certificateStore,
    OioSamlOptions options,
    TimeProvider timeProvider,
    ILogger<IdentityProviderCertificateRefresh> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!options.CertificateRevocation.Enabled)
            logger.LogWarning("Revocation checking of identity provider certificates is disabled; only their validity period is checked.");

        while (true)
        {
            try
            {
                foreach (var problem in await certificateStore.RefreshAsync(cancellationToken).ConfigureAwait(false))
                    logger.LogWarning("Identity provider certificate problem: {Problem}", problem);
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(e, "Revocation check of identity provider certificates failed.");
            }
            await timeProvider.Delay(options.CertificateRevocation.CheckInterval, cancellationToken).ConfigureAwait(false);
        }
    }
}
