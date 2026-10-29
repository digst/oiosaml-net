using DK.Gov.Oio.Saml.AspNet.Controllers;
using DK.Gov.Oio.Saml.AspNet.DependencyInjection;
using DK.Gov.Oio.Saml.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DK.Gov.Oio.Saml.AspNet.Tests;

public class DependencyInjectionTests
{
    private readonly ServiceProviderDependencyResolver _resolver = new(new ServiceCollection().AddOioSaml(TestOptions.Create()).BuildServiceProvider());

    [Fact]
    public void AuthControllerResolvesInARequest()
    {
        FakeHttpContext.Current();
        Assert.IsType<AuthController>(_resolver.GetService(typeof(AuthController)));
    }

    [Fact]
    public void UnregisteredTypeResolvesToNullSoMvcUsesItsDefault() => Assert.Null(_resolver.GetService(typeof(IControllerFactoryStandIn)));

    [Fact]
    public void ScopedServiceIsSharedWithinARequest()
    {
        FakeHttpContext.Current();
        Assert.Same(_resolver.GetService(typeof(ISecurityContext)), _resolver.GetService(typeof(ISecurityContext)));
    }

    [Fact]
    public void ScopedServiceIsNewForEachRequest()
    {
        FakeHttpContext.Current();
        var first = _resolver.GetService(typeof(ISecurityContext));
        FakeHttpContext.Current();
        Assert.NotSame(first, _resolver.GetService(typeof(ISecurityContext)));
    }

    private interface IControllerFactoryStandIn;
}
