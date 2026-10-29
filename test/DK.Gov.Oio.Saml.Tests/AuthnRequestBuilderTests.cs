using System.Xml;
using DK.Gov.Oio.Saml.Builders;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Constants;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Extensions;
using DK.Gov.Oio.Saml.Interfaces;
using DK.Gov.Oio.Saml.Schema.Core;
using DK.Gov.Oio.Saml.Schema.Protocol;
using Moq;

namespace DK.Gov.Oio.Saml.Tests;

public class AuthnRequestBuilderTests
{
    private readonly Saml20AuthnRequestBuilder _cut;
    
    private readonly OioSamlOptions _options = CreateOptions();
    private readonly IdentityProviderMetadata _identityProviderMetadata = CreateIdentityProviderMetadata();
    
    private Saml20AuthnRequest? _authnRequest;
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    public AuthnRequestBuilderTests()
    {
        var identityProviderMetadataServiceMock = new Mock<IIdentityProviderMetadataService>();
        identityProviderMetadataServiceMock.Setup(x => x.GetMetadata()).Returns(_identityProviderMetadata);

        var authnRequestProcesserMock = new Mock<IAuthnRequestProcessor>();
        authnRequestProcesserMock.Setup(x => x.Process(It.IsAny<Saml20AuthnRequest>()))
            .Callback<Saml20AuthnRequest>(req => _authnRequest = req);
        
        var time = new Mock<TimeProvider>();
        time.Setup(t => t.GetUtcNow()).Returns(Now);
        _cut = new Saml20AuthnRequestBuilder(_options,
            identityProviderMetadataServiceMock.Object,
            [authnRequestProcesserMock.Object],
            time.Object);
    }

    [Fact]
    public void NameIdPolicyAllowCreate_IsAnXmlBoolean()
    {
        var request = Execute();
        request.Request.NameIDPolicy = new NameIDPolicy { AllowCreate = true };

        Assert.Equal("true", request.GetXml().DocumentElement!["NameIDPolicy", Saml20Constants.PROTOCOL]!.GetAttribute("AllowCreate"));
    }

    [Fact]
    public void IssueInstant_ComesFromTheTimeProvider() => Assert.Equal(Now.UtcDateTime, Execute().Request.IssueInstant);

    [Fact]
    public void DestinationShouldBeIdPEndpoint()
    {
        var actual = Execute();
        
        Assert.Equal(_identityProviderMetadata.SingleSignOnService.Location, actual.Destination);
    }
    [Fact]
    public void ShouldSetIssuer_FromConfiguration()
    {
        var actual = Execute();
        
        Assert.Equal(_options.ServiceProvider.EntityId, actual.Issuer);
    }

    [Fact]
    public void ShouldAddAudienceRestriction_ForServiceProvider()
    {
        var actual = Execute().GetAudienceRestrictions();
        
        Assert.Contains(_options.ServiceProvider.EntityId, actual);
    }
    
    [Fact]
    public void OneTimeUse_IsNotRequestedByDefault() => Assert.Empty(Execute().Request.Conditions.Items.OfType<OneTimeUse>());

    [Fact]
    public void OneTimeUse_IsRequestedWhenConfigured()
    {
        _options.IdentityProvider.OneTimeUse = true;

        Assert.NotNull(Execute().GetXml().DocumentElement!["Conditions", Saml20Constants.ASSERTION]!["OneTimeUse", Saml20Constants.ASSERTION]);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IfIsPassiveRequested_ShouldBePassive(bool isPassive, bool expected)
    {
        Mock<IAuthnRequestParameters> authnRequestParamsMock= new();
        authnRequestParamsMock.Setup(x => x.IsPassive).Returns(isPassive);
        
        var actual = Execute(authnRequestParamsMock.Object);
        
        Assert.Equal(expected, actual.IsPassive);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IfEndpointIsPassive_ShouldBePassive(bool isPassive, bool expected)
    {
        Mock<IAuthnRequestParameters> authnRequestParamsMock= new();
        authnRequestParamsMock.Setup(x => x.IsPassive).Returns(isPassive);
        
        var actual = Execute(authnRequestParamsMock.Object);
        
        Assert.Equal(expected, actual.IsPassive);
    }
    
    [Theory]
    [InlineData(LoAValues.Low)]
    [InlineData(LoAValues.Substantial)]
    [InlineData(LoAValues.High)]
    public void IfLoaIsRequested_ItShouldBeIncluded(string loa)
    {
        _options.LevelOfAssurance.RequestGenericLoa = true;
        Mock<IAuthnRequestParameters> authnRequestParamsMock= new();
        authnRequestParamsMock.Setup(x => x.Loa).Returns(loa);
        
        var actual = Execute(authnRequestParamsMock.Object);
        
        Assert.NotNull(actual.Request.RequestedAuthnContext);
        
        Assert.NotNull(actual.Request.RequestedAuthnContext.ItemsElementName);
        Assert.Single(actual.Request.RequestedAuthnContext.ItemsElementName);
        Assert.Equal(ItemsChoiceType7.AuthnContextClassRef, actual.Request.RequestedAuthnContext.ItemsElementName[0]);
        
        Assert.NotNull(actual.Request.RequestedAuthnContext.Items);
        Assert.Single(actual.Request.RequestedAuthnContext.Items);
        Assert.Equal(OioSaml4Constants.GenericLoAPrefix + "/" + loa, actual.Request.RequestedAuthnContext.Items[0]);
    }

    [Theory]
    [InlineData(LoAValues.Low)]
    [InlineData(LoAValues.Substantial)]
    [InlineData(LoAValues.High)]
    public void WithoutGenericLoa_TheLoaIsRequestedAsNsis(string loa)
    {
        _options.LevelOfAssurance.RequestGenericLoa = false;
        var parameters = new Mock<IAuthnRequestParameters>();
        parameters.Setup(x => x.Loa).Returns(loa);

        Assert.Equal(OioSaml4Constants.NsisLoAPrefix + "/" + loa, Execute(parameters.Object).Request.RequestedAuthnContext.Items[0]);
    }

    [Fact]
    public void RequestedProfile_IsAnExactAuthnContextClassRef()
    {
        var parameters = new Mock<IAuthnRequestParameters>();
        parameters.Setup(x => x.Profile).Returns(OioSaml3Profiles.Professional);

        var context = Execute(parameters.Object).Request.RequestedAuthnContext;

        Assert.Equal(("https://data.gov.dk/eid/Professional", false), (string.Join(" ", context.Items), context.ComparisonSpecified));
    }

    [Fact]
    public void RequestedProfileAndLoa_AreRequestedWithMinimumComparison()
    {
        _options.LevelOfAssurance.RequestGenericLoa = false;
        var parameters = new Mock<IAuthnRequestParameters>();
        parameters.Setup(x => x.Loa).Returns(LoAValues.Substantial);
        parameters.Setup(x => x.Profile).Returns(OioSaml3Profiles.Person);

        var context = Execute(parameters.Object).Request.RequestedAuthnContext;

        Assert.Equal((OioSaml4Constants.NsisLoAPrefix + "/Substantial https://data.gov.dk/eid/Person", AuthnContextComparisonType.minimum, true),
            (string.Join(" ", context.Items), context.Comparison, context.ComparisonSpecified));
    }

    [Fact]
    public void UnknownProfile_IsRejected()
    {
        var parameters = new Mock<IAuthnRequestParameters>();
        parameters.Setup(x => x.Profile).Returns("Citizen");

        Assert.StartsWith("Received unknown profile: \"Citizen\"", Assert.Throws<Saml20Exception>(() => Execute(parameters.Object)).Message);
    }

    [Fact]
    public void IfInvalidLoaIsRequested_ItShouldBeRejected()
    {
        const string unknownLoa = "Unknown LOA";
        Mock<IAuthnRequestParameters> authnRequestParamsMock= new();
        authnRequestParamsMock.Setup(x => x.Loa).Returns(unknownLoa);
        
        var actual = Assert.Throws<Saml20Exception>( () => Execute(authnRequestParamsMock.Object) );
        
        Assert.StartsWith($"Received unknown LoA: \"{unknownLoa}\"", actual.Message);
    }
    
    [Fact]
    public void IfUnknownAppSwitchPlatformRequested_ItShouldBeRejected()
    {
        var authnRequestParamsMock = new Mock<IAuthnRequestParameters>();
        authnRequestParamsMock.Setup(x => x.AppSwitchPlatform).Returns("SomeUnknownPlatform");
        
        var actual = Assert.Throws<Saml20Exception>( () => Execute(authnRequestParamsMock.Object) );
        
        Assert.StartsWith("AppSwitch platform not recognized. Options are:", actual.Message);
    }
    
    [Fact]
    public void IfUnknownAttributeProfileRequested_ItShouldBeRejected()
    {
        var arrayWithInvalidAttributeProfileElement = new[] { OioSaml4AttributeProfiles.DkPerson, "invalid attribute profile",  OioSaml4AttributeProfiles.EidasPerson};
        var authnRequestParamsMock = new Mock<IAuthnRequestParameters>();
        authnRequestParamsMock.Setup(x => x.AttributeProfiles).Returns(arrayWithInvalidAttributeProfileElement.ToArray());
        
        var actual = Assert.Throws<Saml20Exception>( () => Execute(authnRequestParamsMock.Object) );
        
        Assert.StartsWith($"Unknown attribute profile requested: {arrayWithInvalidAttributeProfileElement[1]}", actual.Message);
    }
    
    [Fact]
    public void IfAttributeProfilesRequested_TheyShouldBeAddedToAuthnRequest()
    {
        var authnRequestParamsMock = new Mock<IAuthnRequestParameters>();
        authnRequestParamsMock.Setup(x => x.AttributeProfiles).Returns(OioSaml4AttributeProfiles.AllProfiles.ToArray());
        
        var actual = Execute(authnRequestParamsMock.Object);
        
        Assert.NotNull(actual.Request.Extensions);
        Assert.NotNull(actual.Request.Extensions.Any);
    
        var requestedAttributeProfiles = actual.Request.Extensions.Any.SingleOrDefault(e => e.LocalName == "RequestedAttributeProfiles");
        Assert.NotNull(requestedAttributeProfiles);

        var profiles = requestedAttributeProfiles.ChildNodes.Cast<XmlNode>().ToList();
        Assert.Equal(OioSaml4AttributeProfiles.AllProfiles.Length, profiles.Count);
        Assert.Equal(OioSaml4AttributeProfiles.AllProfiles.Length, profiles.Count(p => p.LocalName == "Profile"));
        
        foreach (var profile in profiles)
        {
            Assert.True(OioSaml4AttributeProfiles.AllProfiles.Contains(profile.InnerText), $"Invalid profile value {profile.InnerText}");
        }
    }
    
    [Fact]
    public void IfAttributeProfilesAndAppSwitchRequested_TheyShouldBeAddedToAuthnRequest()
    {
        var authnRequestParamsMock = new Mock<IAuthnRequestParameters>();
        authnRequestParamsMock.Setup(x => x.AttributeProfiles).Returns(OioSaml4AttributeProfiles.AllProfiles.ToArray());
        authnRequestParamsMock.Setup(x => x.AppSwitchPlatform).Returns("Android");
        
        var actual = Execute(authnRequestParamsMock.Object);
        
        Assert.NotNull(actual.Request.Extensions);
        Assert.NotNull(actual.Request.Extensions.Any);
    
        var requestedAttributeProfiles = actual.Request.Extensions.Any.SingleOrDefault(e => e.LocalName == "RequestedAttributeProfiles");
        Assert.NotNull(requestedAttributeProfiles);
        
        var profiles = requestedAttributeProfiles.ChildNodes.Cast<XmlNode>().ToList();
        Assert.Equal(OioSaml4AttributeProfiles.AllProfiles.Length, profiles.Count(p => p.LocalName == "Profile"));
        
        var appSwitchElement = actual.Request.Extensions.Any.SingleOrDefault(e => e.LocalName == "AppSwitch");
        Assert.NotNull(appSwitchElement);
        
        var appSwitchChildElements = appSwitchElement.ChildNodes.Cast<XmlElement>().ToList();
        Assert.Equal(2, appSwitchChildElements.Count);
    }
 
    [Theory]
    [InlineData("Android", "Android", "https://android.url")]
    [InlineData("iOS", "iOS", "https://ios.url")]
    public void IfAppSwitchPlatformIsRequested_ItShouldBeAddedToAuthnRequest(string appSwitchPlatform, string expectedPlatformInXml, string expectedUrlInXml)
    {
        var authnRequestParamsMock = new Mock<IAuthnRequestParameters>();
        authnRequestParamsMock.Setup(x => x.AppSwitchPlatform).Returns(appSwitchPlatform);
        
        var actual = Execute(authnRequestParamsMock.Object);
        
        Assert.NotNull(actual.Request.Extensions);
        Assert.NotNull(actual.Request.Extensions.Any);
        
        var appSwitchElement = actual.Request.Extensions.Any.SingleOrDefault(e => e.LocalName == "AppSwitch");
        Assert.NotNull(appSwitchElement);
        
        var children = appSwitchElement.ChildNodes.Cast<XmlElement>().ToList();
        Assert.Equal(2, children.Count);
        
        var platform = children.FirstOrDefault(e => e.LocalName == "Platform");
        Assert.NotNull(platform);
        Assert.Equal(expectedPlatformInXml, platform.InnerText);
        
        var returnUrl = children.FirstOrDefault(e => e.LocalName == "ReturnURL");
        Assert.NotNull(returnUrl);
        Assert.Equal(expectedUrlInXml, returnUrl.InnerText);
    }

    [Fact]
    public void ByDefault_TheConfiguredMinimumLoaIsRequested()
    {
        UseDefaults();

        Assert.Equal(OioSaml4Constants.GenericLoAPrefix + "/Substantial", RequestedClassRefs(Execute()));
    }

    [Fact]
    public void WithoutGenericLoa_TheConfiguredNsisMinimumIsRequested()
    {
        UseDefaults();
        _options.LevelOfAssurance.RequestGenericLoa = false;
        _options.LevelOfAssurance.MinimumNsisLoa = LoAValues.High;

        Assert.Equal(OioSaml4Constants.NsisLoAPrefix + "/High", RequestedClassRefs(Execute()));
    }

    [Fact]
    public void RequestMinimumLoaOff_RequestsNoLoa()
    {
        UseDefaults();
        _options.LevelOfAssurance.RequestMinimumLoa = false;

        Assert.Null(Execute().Request.RequestedAuthnContext);
    }

    [Fact]
    public void ByDefault_TheLoaFromTheRequestIsIgnored()
    {
        UseDefaults();

        Assert.Equal(OioSaml4Constants.GenericLoAPrefix + "/Substantial", RequestedClassRefs(Execute(EverythingFromTheBrowser())));
    }

    [Fact]
    public void ByDefault_ForceAuthnAndIsPassiveFromTheRequestAreIgnored()
    {
        UseDefaults();

        var request = Execute(EverythingFromTheBrowser());

        Assert.Equal((false, false), (request.ForceAuthn, request.IsPassive));
    }

    [Fact]
    public void ByDefault_AttributeProfilesFromTheRequestAreIgnored()
    {
        UseDefaults();

        Assert.Null(Execute(EverythingFromTheBrowser()).Request.Extensions);
    }

    [Fact]
    public void ConfiguredProfile_IsRequested()
    {
        UseDefaults();
        _options.Login.Profile = OioSaml3Profiles.Person;

        Assert.Equal(OioSaml4Constants.GenericLoAPrefix + "/Substantial https://data.gov.dk/eid/Person", RequestedClassRefs(Execute(EverythingFromTheBrowser())));
    }

    [Fact]
    public void ConfiguredAttributeProfiles_AreRequested()
    {
        UseDefaults();
        _options.Login.AttributeProfiles = [OioSaml4AttributeProfiles.DkProfessional];

        Assert.Equal([OioSaml4AttributeProfiles.DkProfessional], Execute(EverythingFromTheBrowser()).Request.GetAttributeProfiles());
    }

    [Fact]
    public void RequestedLoaBelowTheConfiguredMinimum_IsRefused()
    {
        _options.LevelOfAssurance.MinimumLoa = LoAValues.Substantial;
        var parameters = new Mock<IAuthnRequestParameters>();
        parameters.Setup(x => x.Loa).Returns(LoAValues.Low);

        Assert.StartsWith("[OIO-SP-16] Requested LoA Low is below the configured minimum Substantial", Assert.Throws<Saml20Exception>(() => Execute(parameters.Object)).Message);
    }

    [Fact]
    public void ConfiguredProfile_YieldsToTheRequestWhenAllowed()
    {
        _options.Login.Profile = OioSaml3Profiles.Person;

        Assert.Equal("https://data.gov.dk/eid/Professional", RequestedClassRefs(Execute(EverythingFromTheBrowser())).Split(' ').Last());
    }

    [Fact]
    public void ShouldAdd_IdPEndpoint_To_Saml20AuthnRequest()
    {
        var actual = Execute();
        
        Assert.Same(_options.IdentityProvider, actual.Endpoint);
    }

    [Fact]
    public void ShouldAdd_IdPMetadata_To_Saml20AuthnRequest()
    {
        var actual = Execute();
        
        Assert.Same(_identityProviderMetadata, actual.EndpointMetadata);
    }

    private Saml20AuthnRequest Execute()
    {
        var authnRequestParamsMock = new Mock<IAuthnRequestParameters>();
        return _cut.CreateSamlAuthnRequest(authnRequestParamsMock.Object);
    }
    private Saml20AuthnRequest Execute(IAuthnRequestParameters authnRequestParams)
    {
         return _cut.CreateSamlAuthnRequest(authnRequestParams);
    }
    
    // Most tests here exercise the login parameters, which only a test SP takes from the request.
    private static OioSamlOptions CreateOptions()
    {
        return new OioSamlOptions
        {
            ServiceProvider = CreateServiceProviderConfig(),
            IdentityProvider = new IdentityProvider { EntityId = "https://saml.idp.id" },
            AppSwitchReturnUrls = CreateAppSwitchReturnUrlConfig(),
            LevelOfAssurance = new LevelOfAssuranceOptions { MinimumLoa = LoAValues.Low, RequestMinimumLoa = false },
            Login = new LoginOptions { AllowParametersFromRequest = true },
        };
    }

    private void UseDefaults()
    {
        _options.LevelOfAssurance = new LevelOfAssuranceOptions();
        _options.Login = new LoginOptions();
    }

    private static IAuthnRequestParameters EverythingFromTheBrowser()
    {
        var parameters = new Mock<IAuthnRequestParameters>();
        parameters.Setup(x => x.Loa).Returns(LoAValues.High);
        parameters.Setup(x => x.Profile).Returns(OioSaml3Profiles.Professional);
        parameters.Setup(x => x.AttributeProfiles).Returns([OioSaml4AttributeProfiles.DkPerson]);
        parameters.Setup(x => x.ForceAuthn).Returns(true);
        parameters.Setup(x => x.IsPassive).Returns(true);
        return parameters.Object;
    }

    private static string RequestedClassRefs(Saml20AuthnRequest request) => string.Join(" ", request.Request.RequestedAuthnContext?.Items ?? []);

    private static List<AppSwitchReturnUrl> CreateAppSwitchReturnUrlConfig()
    {
        return new AppSwitchReturnUrl[]
        {
            new() {
                Platform = Platform.Android,
                Value = "https://android.url"
            },
            new() {
                Platform = Platform.iOS,
                Value = "https://ios.url"
            }
        }.ToList();
    }
    
    private static ServiceProviderOptions CreateServiceProviderConfig()
    {
        return new ServiceProviderOptions { EntityId = "https://saml.entity.id" };
    }

    private static IdentityProviderMetadata CreateIdentityProviderMetadata()
    {
        return new IdentityProviderMetadata("https://saml.idp.id",
            new IdentityProviderEndpointService(Saml20Constants.ProtocolBindings.HTTP_Redirect, "https://saml.idp.id/sso"));
    }
}
