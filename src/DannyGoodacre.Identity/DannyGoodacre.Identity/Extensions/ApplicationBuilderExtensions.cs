using DannyGoodacre.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        public async Task<Result> SeedIdentityAsync(string adminUsername, string adminPassword)
        {
            await using var scope = app.ApplicationServices.CreateAsyncScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Core.IdentityUser>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var adminUser = await userManager.FindByNameAsync(adminUsername);

            if (adminUser is not null)
            {
                return Result.Success();
            }

            adminUser = new Core.IdentityUser
            {
                UserName = adminUsername,
                EmailConfirmed = true
            };

            var userResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!userResult.Succeeded)
            {
                return Result.InternalError("User creation failed.");
            }

            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");

            return roleResult.Succeeded
                ? Result.Success()
                : Result.InternalError("Role creation failed.");
        }
    }
}
