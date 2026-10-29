using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Tests.Builders;

namespace DK.Gov.Oio.Saml.Tests;

public class RedirectBindingSigningTests
{
    [Fact]
    public void AuthnRequestQuery_IsSignedWithTheServiceProviderKey()
    {
        var builder = new HttpRedirectRequestBuilder(TestFederation.Options());
        var authnRequest = new Saml20AuthnRequest(new IdentityProvider { EntityId = TestFederation.IdpEntityId }, TestFederation.MetadataService().GetMetadata(), DateTime.UtcNow)
        {
            Destination = "https://idp.test/sso",
            Issuer = TestFederation.SpEntityId,
        };

        var url = builder.GetSignedAuthnRequest(authnRequest, null);

        var query = url.Substring(url.IndexOf('?') + 1);
        var signatureStart = query.IndexOf("&Signature=", StringComparison.Ordinal);
        var signedData = Encoding.UTF8.GetBytes(query.Substring(0, signatureStart));
        var signature = Convert.FromBase64String(HttpUtility.UrlDecode(query.Substring(signatureStart + "&Signature=".Length)));
        Assert.StartsWith("https://idp.test/sso?SAMLRequest=", url);
        Assert.True(TestCertificates.Sp.GetRSAPublicKey()!.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
}
