using DannyGoodacre.Identity.Application;
using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIdentity<TContext>(Action<CookieAuthenticationOptions>? configureOptions = null)
            where TContext : IdentityContext
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "DannyGoodacre.Identity";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.LoginPath = "/session";

                    configureOptions?.Invoke(options);
                });

            services.AddScoped<ICookieService, CookieService>();

            services.AddData<TContext>();

            services.AddHashing();

            services.AddApplication();

            return services;
        }
    }
}
