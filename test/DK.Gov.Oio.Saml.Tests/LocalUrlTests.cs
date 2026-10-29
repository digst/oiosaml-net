using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Tests;

public class LocalUrlTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/home/secure")]
    [InlineData("/home/secure?tab=1#top")]
    [InlineData("~/")]
    [InlineData("~/home")]
    public void LocalPath_IsLocal(string url) => Assert.True(LocalUrl.IsLocal(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.test/")]
    [InlineData("//evil.test")]
    [InlineData("/\\evil.test")]
    [InlineData("/\t/evil.test")]
    [InlineData("~//evil.test")]
    [InlineData("~/\\evil.test")]
    [InlineData("javascript:alert(1)")]
    [InlineData("evil.test")]
    [InlineData("~")]
    public void ExternalOrMalformedUrl_IsNotLocal(string? url) => Assert.False(LocalUrl.IsLocal(url));
}
