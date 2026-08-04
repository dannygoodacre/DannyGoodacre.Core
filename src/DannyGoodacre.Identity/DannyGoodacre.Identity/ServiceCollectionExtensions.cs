using DannyGoodacre.Identity.Application;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Identity.Security;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CookieOptions = DannyGoodacre.Identity.Configuration.CookieOptions;

namespace DannyGoodacre.Identity;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIdentity<TContext>(IConfiguration? configuration = null,
                                                        Action<IdentityOptions>? configureOptions = null)
            where TContext : IdentityContext
        {
            services.Configure<IdentityOptions>(options =>
            {
                configuration?.GetSection("Identity").Bind(options);

                configureOptions?.Invoke(options);
            });

            services.AddSingleton<IIdentityPermissionRegistry, IdentityPermissionRegistry>();

            services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            services.AddScoped<SecurityStampValidatorService>();

            services.AddScoped<IClaimService, ClaimService>();

            services.AddScoped<ICookieService, CookieService>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

            services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
                .Configure<IOptions<IdentityOptions>>((cookieAuthOptions, identityOptions) =>
                {
                    CookieOptions cookieSettings = identityOptions.Value.Cookie;

                    cookieAuthOptions.Cookie.Name = cookieSettings.Name;
                    cookieAuthOptions.Cookie.HttpOnly = cookieSettings.HttpOnly;
                    cookieAuthOptions.Cookie.SecurePolicy = cookieSettings.SecurePolicy;
                    cookieAuthOptions.Cookie.SameSite = cookieSettings.SameSite;
                    cookieAuthOptions.LoginPath = cookieSettings.LoginPath;
                    cookieAuthOptions.ExpireTimeSpan = cookieSettings.ExpireTimeSpan;
                    cookieAuthOptions.SlidingExpiration = cookieSettings.SlidingExpiration;

                    cookieAuthOptions.EventsType = typeof(SecurityStampValidatorService);
                });

            services.AddData<TContext>();

            services.AddHashingService();

            services.AddApplication();

            return services;
        }
    }
}
