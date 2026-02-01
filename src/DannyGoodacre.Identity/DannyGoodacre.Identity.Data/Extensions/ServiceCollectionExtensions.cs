using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Data.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddData<TContext>() where TContext : IdentityContext
            => services
                .AddScoped<IdentityContext, TContext>()
                .AddScoped<IUserRepository, UserRepository>()
                .AddScoped<IRoleRepository, RoleRepository>();
    }
}
