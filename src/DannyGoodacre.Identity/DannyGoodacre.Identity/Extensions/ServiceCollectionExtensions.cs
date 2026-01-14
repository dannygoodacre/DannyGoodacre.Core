using System.Reflection;
using DannyGoodacre.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIdentity<TContext>(Action<IdentityOptions>? setupAction = null)
            where TContext : IdentityContext
        {
            services.AddIdentity<IdentityUser, IdentityRole>(setupAction ?? (_ => {}))
                    .AddEntityFrameworkStores<TContext>();

            services.AddCommandHandlers(Assembly.GetExecutingAssembly())
                    .AddQueryHandlers(Assembly.GetExecutingAssembly());

            services.AddScoped<IdentityContext>(provider => provider.GetRequiredService<TContext>());

            return services;
        }
    }
}
