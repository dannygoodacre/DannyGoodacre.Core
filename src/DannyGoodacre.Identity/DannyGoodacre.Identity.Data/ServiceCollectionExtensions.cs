using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Data;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddData<TContext>() where TContext : IdentityContext
        {
            services.AddScoped<IdentityContext, TContext>();

            services.AddScoped<IStateUnit>(x => x.GetRequiredService<IdentityContext>());

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IRoleRepository, RoleRepository>();

            return services;
        }
    }
}
