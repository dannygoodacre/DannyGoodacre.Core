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

            if (!await roleManager.RoleExistsAsync("User"))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole("User"));

                if (!createRoleResult.Succeeded)
                {
                    return Result.InternalError("User role creation failed.");
                }
            }

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (!createRoleResult.Succeeded)
                {
                    return Result.InternalError("Admin role creation failed.");
                }
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

            var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!createUserResult.Succeeded)
            {
                return Result.InternalError("User creation failed.");
            }

            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");

            return addToRoleResult.Succeeded
                ? Result.Success()
                : Result.InternalError("Role creation failed.");
        }
    }
}
