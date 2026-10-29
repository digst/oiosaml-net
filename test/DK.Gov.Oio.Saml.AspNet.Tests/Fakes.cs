using System.Collections;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Web;
using DK.Gov.Oio.Saml.Configuration;
using DK.Gov.Oio.Saml.Tests.Builders;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Moq;

namespace DK.Gov.Oio.Saml.AspNet.Tests;

public sealed class FakeSession : HttpSessionStateBase
{
    private readonly Dictionary<string, object?> _values = [];

    public override object? this[string name]
    {
        get => _values.TryGetValue(name, out var value) ? value : null;
        set => _values[name] = value;
    }

    public override void Remove(string name) => _values.Remove(name);
}

public sealed class FixedClock(DateTimeOffset now) : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

// A request as MVC sees it: query string and form from Request.Unvalidated, and an OWIN environment for sign-in.
public sealed class FakeHttpContext
{
    public NameValueCollection Query { get; } = [];
    public NameValueCollection Form { get; } = [];
    public string RawUrl { get; set; } = "/auth/logout";
    public HttpSessionStateBase? Session { get; set; } = new FakeSession();
    public ClaimsPrincipal? User { get; set; }
    public OwinContext Owin { get; } = new();

    public HttpContextBase Build()
    {
        var unvalidated = new Mock<UnvalidatedRequestValuesBase>();
        unvalidated.Setup(u => u.QueryString).Returns(Query);
        unvalidated.Setup(u => u.Form).Returns(Form);
        var request = new Mock<HttpRequestBase>();
        request.Setup(r => r.Unvalidated).Returns(unvalidated.Object);
        request.Setup(r => r.RawUrl).Returns(() => RawUrl);
        request.Setup(r => r.UserHostAddress).Returns("192.0.2.1");
        var context = new Mock<HttpContextBase>();
        context.Setup(c => c.Request).Returns(request.Object);
        context.Setup(c => c.Session).Returns(() => Session!);
        context.Setup(c => c.User).Returns(() => User!);
        context.Setup(c => c.Items).Returns(new Hashtable { ["owin.Environment"] = Owin.Environment });
        return context.Object;
    }

    // System.Web's own HttpContext, for code that reads HttpContext.Current, such as MVC's AntiForgery.
    public static HttpContext Current(string url = "https://localhost:20002/") =>
        HttpContext.Current = new HttpContext(new HttpRequest("", url, ""), new HttpResponse(TextWriter.Null));
}

public static class TestOptions
{
    public static OioSamlOptions Create()
    {
        var options = new OioSamlOptions
        {
            ServiceProvider = new ServiceProviderOptions { EntityId = "https://sp.test", Server = "https://sp.test" },
            IdentityProvider = new IdentityProvider { EntityId = "https://idp.test", MetadataFile = "idp-metadata/test-idp.xml" },
        };
        options.ServiceProvider.SetCertificate(TestCertificates.Sp);
        return options;
    }
}
