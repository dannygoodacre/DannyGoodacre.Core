using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Core.Identity;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIdentity<TContext>(Action<IdentityOptions>? setupAction = null)
            where TContext : IdentityContext
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(setupAction ?? (_ => {}))
                    .AddEntityFrameworkStores<TContext>();

            services.AddCommandHandlers(Assembly.GetExecutingAssembly())
                    .AddQueryHandlers(Assembly.GetExecutingAssembly());

            services.AddScoped<IdentityContext>(provider => provider.GetRequiredService<TContext>());

            services.AddScoped<IUnitOfWorkWithTransaction, EfUnitOfWork>();

            return services;
        }
    }
}
