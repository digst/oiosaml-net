using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace DK.Gov.Oio.Saml.Certificates;

public interface ICertificateValidator
{
    Task<CertificateValidationResult> ValidateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);
}
