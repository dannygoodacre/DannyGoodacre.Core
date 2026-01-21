using System.Reflection;
using DannyGoodacre.Core;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity.Application;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
            => services
                .AddCommandHandlers(Assembly.GetExecutingAssembly())
                .AddQueryHandlers(Assembly.GetExecutingAssembly());
    }
}
