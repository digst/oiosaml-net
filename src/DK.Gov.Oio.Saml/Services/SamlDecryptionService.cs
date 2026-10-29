using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Interfaces;

namespace DK.Gov.Oio.Saml.Services;

internal class SamlDecryptionService(OioSamlOptions options) : ISamlDecryptionService
{
    public XmlElement Decrypt(XmlElement encryptedAssertionNode)
    {
        var certificate = options.ServiceProvider.GetCertificate();
        using var key = certificate.GetRSAPrivateKey() ?? throw new Saml20Exception($"Certificate {certificate.Thumbprint} has no RSA private key.");
        try { return new Saml20EncryptedAssertion(key, encryptedAssertionNode).Decrypt(); }
        catch (CryptographicException e)
        {
            throw new Saml20Exception($"Could not decrypt the assertion with service provider certificate {certificate.Thumbprint}. It was probably encrypted for a different certificate, e.g. after NemLog-in switched to new SP metadata.", e);
        }
    }
}
