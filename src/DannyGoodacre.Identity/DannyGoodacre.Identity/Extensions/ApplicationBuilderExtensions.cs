using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        public async Task SeedIdentityAsync<TUser, TRole>(string adminUsername, string adminPassword)
            where TUser : IdentityUser
            where TRole : IdentityRole
        {
            await using var scope = app.ApplicationServices.CreateAsyncScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        }
    }
}
