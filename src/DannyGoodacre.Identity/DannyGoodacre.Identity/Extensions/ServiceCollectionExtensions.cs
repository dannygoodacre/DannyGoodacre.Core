using DannyGoodacre.Identity.Application;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        // TODO: Is setupAction needed yet?
        public IServiceCollection AddIdentity<TContext>(Action<IdentityOptions>? setupAction = null)
            where TContext : IdentityContext
        {
            services.AddHttpContextAccessor();

            services
                .AddIdentity<Core.IdentityUser, IdentityRole>(setupAction ?? (_ => { }))
                .AddEntityFrameworkStores<TContext>();

            services
                .AddScoped<Application.Abstractions.ISignInManager, SignInManager>()
                .AddScoped<Application.Abstractions.IUserContext, UserContext>()
                .AddScoped<Application.Abstractions.IUserManager<Core.IdentityUser>, UserManager>()
                .AddScoped<Application.Abstractions.IUserStore<Core.IdentityUser>, UserStore>();

            services.AddApplication();

            services.AddScoped<IdentityContext>(provider => provider.GetRequiredService<TContext>());

            return services;
        }
    }
}
