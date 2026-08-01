using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Data;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddData<TContext>()
            where TContext : IdentityContext
        {
            services.AddScoped<IdentityContext, TContext>();

            services.AddEntityFrameworkUnits<IdentityContext>();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IRoleRepository, RoleRepository>();

            services.AddScoped<IClaimRepository, ClaimRepository>();

            services.AddScoped<IUserClaimRepository, UserClaimRepository>();

            return services;
        }
    }
}
