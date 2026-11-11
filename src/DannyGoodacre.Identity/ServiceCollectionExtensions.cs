using DannyGoodacre.Identity.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DannyGoodacre.Identity;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers ASP.NET Core Identity with the EF Core stores on <typeparamref name="TContext"/>, cookie sign-in,
        /// and the <see cref="IdentityDefaults.AdminPolicy"/> policy. Configure Identity itself afterwards with the
        /// usual <c>Configure&lt;IdentityOptions&gt;</c> and <c>ConfigureApplicationCookie</c>.
        /// </summary>
        /// <param name="configuration">
        /// The application configuration. <see cref="IdentityProviderOptions"/> is bound from its <c>Identity</c>
        /// section.
        /// </param>
        /// <param name="configure">Configures <see cref="IdentityProviderOptions"/> after binding.</param>
        /// <returns>Identity's own builder, for further standard Identity configuration.</returns>
        public IdentityBuilder AddUsernameIdentity<TContext>(
            IConfiguration? configuration = null,
            Action<IdentityProviderOptions>? configure = null)
            where TContext : IdentityContext
        {
            var optionsBuilder = services.AddOptions<IdentityProviderOptions>();

            if (configuration is not null)
            {
                optionsBuilder.Bind(configuration.GetSection(IdentityProviderOptions.SectionName));
            }

            if (configure is not null)
            {
                optionsBuilder.Configure(configure);
            }

            var identity = services.AddIdentityCore<User>()
                .AddRoles<Role>()
                .AddEntityFrameworkStores<TContext>()
                .AddSignInManager()
                .AddTokenProvider<DataProtectorTokenProvider<User>>(TokenOptions.DefaultProvider);

            // Admin changes, disabling, and password resets reach existing sessions at this interval. Identity's default
            // is 30 minutes; a user base this small can afford to check the database every minute.
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

            services.AddAuthentication(IdentityConstants.ApplicationScheme)
                .AddIdentityCookies();

            // These are API endpoints, so answer with status codes instead of redirecting to a login page.
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context => SetStatusCode(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => SetStatusCode(context, StatusCodes.Status403Forbidden);
            });

            services.AddAuthorizationBuilder()
                .AddPolicy(IdentityDefaults.AdminPolicy, policy => policy.RequireRole(IdentityDefaults.AdminRole));

            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<IIdentityContextAccessor, IdentityContextAccessor<TContext>>();

            return identity;
        }
    }

    private static Task SetStatusCode(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
