using System.Collections.Specialized;
using DK.Gov.Oio.Saml.AspNet.Model;
using DK.Gov.Oio.Saml.Constants;

namespace DK.Gov.Oio.Saml.AspNet.Tests;

public class RequestModelTests
{
    [Fact]
    public void LevelOfAssuranceParameterBecomesLoa() =>
        Assert.Equal(LoAValues.High, AuthnRequestParameters.From(new NameValueCollection { ["levelOfAssurance"] = LoAValues.High }).Loa);

    [Fact]
    public void UnselectedLevelOfAssuranceIsNoLoa() => Assert.Null(AuthnRequestParameters.From(new NameValueCollection { ["levelOfAssurance"] = "" }).Loa);

    [Fact]
    public void EmptyRelayStateIsNoRelayState() => Assert.Null(AuthnRequestParameters.From(new NameValueCollection { ["RelayState"] = "" }).RelayState);

    [Fact]
    public void EmptyAttributeProfileIsIgnored() => Assert.Empty(AuthnRequestParameters.From(new NameValueCollection { ["attributeProfiles"] = "" }).AttributeProfiles);

    [Fact]
    public void EmptySamlRequestIsNoLogoutRequest() => Assert.Null(LogoutParameters.FromForm(new NameValueCollection { ["SAMLRequest"] = "" }).LogoutRequest);

    [Fact]
    public void EveryAttributeProfileIsRead()
    {
        var query = new NameValueCollection { { "attributeProfiles", OioSaml4AttributeProfiles.DkPerson }, { "attributeProfiles", OioSaml4AttributeProfiles.DkProfessional } };
        Assert.Equal([OioSaml4AttributeProfiles.DkPerson, OioSaml4AttributeProfiles.DkProfessional], AuthnRequestParameters.From(query).AttributeProfiles);
    }

    [Fact]
    public void NoAttributeProfilesIsAnEmptyList() => Assert.Empty(AuthnRequestParameters.From([]).AttributeProfiles);

    [Fact]
    public void BooleanParameterIsRead() => Assert.True(AuthnRequestParameters.From(new NameValueCollection { ["forceAuthn"] = "true" }).ForceAuthn);

    [Fact]
    public void UnparsableBooleanIsIgnored() => Assert.Null(AuthnRequestParameters.From(new NameValueCollection { ["isPassive"] = "maybe" }).IsPassive);

    [Fact]
    public void QueryLogoutUsesTheRedirectBinding() =>
        Assert.Equal(Saml20Constants.ProtocolBindings.HTTP_Redirect, LogoutParameters.FromQuery([], "SAMLRequest=x").Binding);

    [Fact]
    public void QueryLogoutKeepsTheRawQuery() => Assert.Equal("SAMLRequest=x", LogoutParameters.FromQuery([], "SAMLRequest=x").RawQuery);

    [Fact]
    public void FormLogoutUsesThePostBinding() => Assert.Equal(Saml20Constants.ProtocolBindings.HTTP_Post, LogoutParameters.FromForm([]).Binding);

    [Fact]
    public void FormLogoutHasNoRawQuery() => Assert.Null(LogoutParameters.FromForm([]).RawQuery);

    [Fact]
    public void LoginResponseIsReadFromTheForm() =>
        Assert.Equal("response", SamlLoginResponse.From(new NameValueCollection { ["SAMLResponse"] = "response" }).LoginResponse);
}
