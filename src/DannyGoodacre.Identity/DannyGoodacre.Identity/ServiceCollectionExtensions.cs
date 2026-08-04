using DannyGoodacre.Identity.Application;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Security;
using DannyGoodacre.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
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
                .AddCookie(options =>
                {
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                        return Task.CompletedTask;
                    };

                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;

                        return Task.CompletedTask;
                    };
                });

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

            services.AddApplication();

            return services;
        }
    }
}
