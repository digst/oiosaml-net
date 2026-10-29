using System.Configuration;
using DK.Gov.Oio.Saml.Configuration;

namespace DK.Gov.Oio.Saml.Tests;

public class ConfiguredPathTests
{
    [Fact]
    public void RelativePath_IsResolvedAgainstTheApplicationFolder() =>
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "idp-metadata"), ConfiguredPath.Resolve("idp-metadata"));

    [Fact]
    public void AbsolutePath_IsKept()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "metadata");

        Assert.Equal(absolute, ConfiguredPath.Resolve(absolute));
    }

    [Fact]
    public void RelativeMetadataFile_IsResolvedAgainstTheApplicationFolder() =>
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "idp-metadata/idp.xml"), new IdentityProvider { MetadataFile = "idp-metadata/idp.xml" }.GetMetadataFile());

    [Fact]
    public void RelativeCertificateFile_IsLookedForInTheApplicationFolder()
    {
        var e = Assert.Throws<ConfigurationErrorsException>(() => new CertificateSource { FilePath = "certificates/missing.p12", Password = "x" }.Load());

        Assert.Contains(Path.Combine(AppContext.BaseDirectory, "certificates/missing.p12"), e.Message);
    }
}
