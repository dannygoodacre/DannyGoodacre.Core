using DannyGoodacre.Identity.Application;
using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Identity.Security;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
            services.AddSingleton<IIdentityPermissionRegistry, IdentityPermissionRegistry>();

            services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            services.AddScoped<SecurityStampValidatorService>();

            services.AddScoped<IClaimService, ClaimService>();

            services.AddScoped<ICookieService, CookieService>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "DannyGoodacre.Identity";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.LoginPath = "/session";

                    options.EventsType = typeof(SecurityStampValidatorService);

                    configureOptions?.Invoke(options);
                });

            services.AddScoped<ICookieService, CookieService>();

            services.AddData<TContext>();

            services.AddHashingService();

            services.AddApplication();

            return services;
        }
    }
}
