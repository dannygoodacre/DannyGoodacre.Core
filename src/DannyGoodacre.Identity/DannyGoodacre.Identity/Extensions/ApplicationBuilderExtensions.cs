using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        public async Task SeedIdentityAsync(string adminUsername, string adminPassword)
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
                return;
            }

            adminUser = new Core.IdentityUser
            {
                UserName = adminUsername,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
