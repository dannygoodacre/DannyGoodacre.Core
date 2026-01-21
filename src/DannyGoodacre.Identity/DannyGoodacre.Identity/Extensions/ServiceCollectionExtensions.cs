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
        public IServiceCollection AddIdentity<TContext>() where TContext : IdentityContext
        {
            services.AddHttpContextAccessor();

            services
                .AddIdentity<Core.IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<TContext>();

            if (typeof(TContext) != typeof(IdentityContext))
            {
                services.AddScoped<IdentityContext>(provider => provider.GetRequiredService<TContext>());
            }

            services
                .AddScoped<Application.Abstractions.ISignInManager, SignInManager>()
                .AddScoped<Application.Abstractions.IUserContext, UserContext>()
                .AddScoped<Application.Abstractions.IUserManager<Core.IdentityUser>, UserManager>()
                .AddScoped<Application.Abstractions.IUserStore<Core.IdentityUser>, UserStore>();

            services.AddApplication();

            return services;
        }
    }
}
