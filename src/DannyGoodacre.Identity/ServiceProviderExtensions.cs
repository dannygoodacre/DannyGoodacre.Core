using DannyGoodacre.Identity.Accounts;
using DannyGoodacre.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity;

public static class ServiceProviderExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        /// <summary>
        /// Prepares the database at startup, in one transaction: ensures the <see cref="IdentityDefaults.AdminRole"/>
        /// role and an invite code exist, and creates <see cref="IdentityProviderOptions.InitialAdmin"/> if no user
        /// has that username.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// An Identity operation failed, or there is no enabled admin and
        /// <see cref="IdentityProviderOptions.InitialAdmin"/> is not set. Nothing is saved.
        /// </exception>
        public async Task InitializeIdentityAsync(CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<IIdentityContextAccessor>().Context;
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<Role>>();
            var options = services.GetRequiredService<IOptions<IdentityProviderOptions>>().Value;

            await context.ExecuteInTransactionAsync(
                async token =>
                {
                    if (!await roleManager.RoleExistsAsync(IdentityDefaults.AdminRole))
                    {
                        Check(await roleManager.CreateAsync(new Role { Name = IdentityDefaults.AdminRole }));
                    }

                    if (await context.Set<InviteCode>().FindAsync([InviteCode.SingletonId], token) is null)
                    {
                        context.Add(new InviteCode { Code = Guid.NewGuid() });
                        await context.SaveChangesAsync(token);
                    }

                    await EnsureAdminAsync(userManager, options.InitialAdmin);

                    return true;
                },
                shouldCommit: committed => committed,
                cancellationToken);
        }
    }

    private static async Task EnsureAdminAsync(UserManager<User> userManager, InitialAdminOptions? initialAdmin)
    {
        if (initialAdmin is { Username.Length: > 0 })
        {
            if (await userManager.FindByNameAsync(initialAdmin.Username) is not null)
            {
                return;
            }

            var admin = new User
            {
                UserName = initialAdmin.Username,
                Email = string.IsNullOrWhiteSpace(initialAdmin.Email) ? null : initialAdmin.Email
            };

            Check(await userManager.CreateAsync(admin, initialAdmin.Password));
            Check(await userManager.AddToRoleAsync(admin, IdentityDefaults.AdminRole));

            return;
        }

        if (await userManager.CountEnabledAdminsAsync() == 0)
        {
            throw new InvalidOperationException(
                $"There is no enabled user in the '{IdentityDefaults.AdminRole}' role. "
                + $"Set '{IdentityProviderOptions.SectionName}:{nameof(IdentityProviderOptions.InitialAdmin)}' to create one.");
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Identity initialization failed: "
                + string.Join(" ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
        }
    }
}
