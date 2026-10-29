using System.Web;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DK.Gov.Oio.Saml.AspNet.DependencyInjection;

// Resolves MVC's services and controllers from a Microsoft.Extensions.DependencyInjection container, with one scope per request.
// A type the container does not know resolves to null, so MVC falls back to its defaults and to parameterless constructors.
public class ServiceProviderDependencyResolver(IServiceProvider services) : IDependencyResolver
{
    private static readonly object ScopeKey = new();

    public object? GetService(Type serviceType) => Current.GetService(serviceType);

    public IEnumerable<object> GetServices(Type serviceType) => Current.GetServices(serviceType).OfType<object>();

    private IServiceProvider Current
    {
        get
        {
            var context = HttpContext.Current;
            if (context == null) return services;
            if (context.Items[ScopeKey] is not IServiceScope scope)
            {
                scope = services.CreateScope();
                context.Items[ScopeKey] = scope;
                context.DisposeOnPipelineCompleted(scope);
            }
            return scope.ServiceProvider;
        }
    }
}
