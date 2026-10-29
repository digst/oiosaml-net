using DK.Gov.Oio.Saml.Certificates;
using DK.Gov.Oio.Saml.Tests.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class ServiceProviderCertificateExpiryTests
{
    private static readonly DateTimeOffset NotAfter = DateTimeOffset.UtcNow.AddDays(100);
    private readonly FakeLogger<ServiceProviderCertificateExpiry> _logger = new();

    private FakeLogCollector CheckAt(DateTimeOffset now)
    {
        var options = TestFederation.Options();
        options.ServiceProvider.SetCertificate(TestCertificates.CreateSelfSigned("Expiring SP", NotAfter));
        var time = new Mock<TimeProvider>();
        time.Setup(t => t.GetUtcNow()).Returns(now);
        new ServiceProviderCertificateExpiry(options, time.Object, _logger).Check();
        return _logger.Collector;
    }

    [Fact]
    public void CertificateValidForMoreThan30Days_IsNotLogged()
    {
        Assert.Equal(0, CheckAt(NotAfter.AddDays(-31)).Count);
    }

    [Fact]
    public void CertificateExpiringWithin30Days_IsAWarning()
    {
        Assert.Equal(LogLevel.Warning, CheckAt(NotAfter.AddDays(-29)).LatestRecord.Level);
    }

    [Fact]
    public void Warning_SaysWhenTheCertificateExpires()
    {
        Assert.Contains("(CN=Expiring SP) expires", CheckAt(NotAfter.AddDays(-29)).LatestRecord.Message);
    }

    [Fact]
    public void CertificateThatExpiredWhileRunning_IsAnError()
    {
        Assert.Equal(LogLevel.Error, CheckAt(NotAfter.AddMinutes(1)).LatestRecord.Level);
    }
}
