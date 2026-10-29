using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Tests.Builders;
using DK.Gov.Oio.Saml.Utils;

namespace DK.Gov.Oio.Saml.Tests;

public class XmlSignatureVerificationTests
{
    private static readonly AsymmetricAlgorithm[] IdpKeys = [TestCertificates.Idp.GetRSAPublicKey()!];

    [Fact]
    public void SignedAssertion_Verifies()
    {
        var assertion = new SamlResponseBuilder().BuildAssertion();

        Assert.Null(XmlSignatureUtils.Verify(assertion, IdpKeys));
    }

    [Fact]
    public void UnsignedAssertion_IsReported()
    {
        var assertion = new SamlResponseBuilder { SignAssertion = false }.BuildAssertion();

        Assert.False(XmlSignatureUtils.IsSigned(assertion));
        Assert.Equal("Element is not signed.", XmlSignatureUtils.Verify(assertion, IdpKeys));
    }

    [Fact]
    public void SignatureByUntrustedKey_IsRejected()
    {
        var assertion = new SamlResponseBuilder { SigningCertificate = TestCertificates.Other }.BuildAssertion();

        Assert.Contains("does not verify", XmlSignatureUtils.Verify(assertion, IdpKeys));
    }

    [Fact]
    public void ContentChangedAfterSigning_IsRejected()
    {
        var assertion = new SamlResponseBuilder().BuildAssertion();
        assertion.GetElementsByTagName("NameID", Saml20Constants.ASSERTION)[0]!.InnerText = "mallory";

        Assert.Contains("does not verify", XmlSignatureUtils.Verify(assertion, IdpKeys));
    }

    [Fact]
    public void WrappedSignedAssertion_CannotSatisfyTheReference()
    {
        var forged = new SamlResponseBuilder().BuildAssertion().WrapInForgedAssertion("mallory");

        Assert.Contains("does not verify", XmlSignatureUtils.Verify(forged, IdpKeys));
    }

    [Fact]
    public void ReferenceToAnotherElement_IsRejected()
    {
        var assertion = new SamlResponseBuilder().BuildAssertion();
        assertion.SetAttribute("ID", "_different");

        Assert.Contains("does not point to the signed element", XmlSignatureUtils.Verify(assertion, IdpKeys));
    }

    [Fact]
    public void SignatureMethodOutsideAllowList_IsRejected()
    {
        var assertion = new SamlResponseBuilder { SignatureMethod = SignedXml.XmlDsigRSASHA512Url, DigestMethod = SignedXml.XmlDsigSHA512Url }.BuildAssertion();

        Assert.Contains("[OIO-ALG-01] Signature method", XmlSignatureUtils.Verify(assertion, IdpKeys));
    }

    [Fact]
    public void SignatureNestedBelowTheSignedElement_IsNotASignature()
    {
        var assertion = new SamlResponseBuilder().BuildAssertion();
        var signature = assertion.GetElementsByTagName("Signature", Saml20Constants.XMLDSIG)[0]!;
        assertion.GetElementsByTagName("Subject", Saml20Constants.ASSERTION)[0]!.AppendChild(signature);

        Assert.Equal("Element is not signed.", XmlSignatureUtils.Verify(assertion, IdpKeys));
    }
}

public static class XmlSignatureWrapping
{
    // Classic XML Signature Wrapping: a forged copy carries the original signature and keeps the original,
    // signature stripped, nested inside it so an ID lookup over the whole document still finds signed content.
    public static XmlElement WrapInForgedAssertion(this XmlElement signedAssertion, string nameId)
    {
        var forged = (XmlElement)signedAssertion.CloneNode(true);
        forged.GetElementsByTagName("NameID", Saml20Constants.ASSERTION)[0]!.InnerText = nameId;
        var original = (XmlElement)signedAssertion.CloneNode(true);
        original.RemoveChild(original.GetElementsByTagName("Signature", Saml20Constants.XMLDSIG)[0]!);
        var advice = signedAssertion.OwnerDocument.CreateElement("saml", "Advice", Saml20Constants.ASSERTION);
        advice.AppendChild(original);
        forged.InsertBefore(advice, forged.GetElementsByTagName("Subject", Saml20Constants.ASSERTION)[0]);
        return forged;
    }
}
