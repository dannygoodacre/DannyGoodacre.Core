using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Security;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity;

public static class ServiceProviderExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        public async Task SynchronizeIdentityPermissionsAsync()
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            IServiceProvider services = scope.ServiceProvider;

            IGetAllClaims getAllClaims = services.GetRequiredService<IGetAllClaims>();

            IIdentityPermissionRegistry identityPermissionRegistry = services.GetRequiredService<IIdentityPermissionRegistry>();

            IAddClaims addClaims = services.GetRequiredService<IAddClaims>();

            Result<List<ClaimResponse>> getAllClaimsResult = await getAllClaims.ExecuteAsync();

            if (!getAllClaimsResult.IsSuccess)
            {
                throw new Exception(getAllClaimsResult.Error);
            }

            List<ClaimResponse> existingClaims = getAllClaimsResult.Value!;

            HashSet<string> existingPermissionClaimValues = existingClaims
                .Where(x => x.Type == IdentityPermissionRegistry.PermissionClaimType)
                .Select(x => x.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<ClaimDefinition> missingClaimDefinitions = identityPermissionRegistry.AllPermissions
                .Where(x => !existingPermissionClaimValues.Contains(x))
                .Select(x => new ClaimDefinition
                {
                    Type = IdentityPermissionRegistry.PermissionClaimType,
                    Value = x
                }).ToList();

            Result addClaimsResult = await addClaims.ExecuteAsync(missingClaimDefinitions);

            if (!addClaimsResult.IsSuccess)
            {
                throw new Exception(addClaimsResult.Error);
            }

            // Seed admin user

            var config = services.GetRequiredService<IConfiguration>();

            SeedAdminCredentials? adminSettings = config.GetSection("Identity:InitialSuperUser").Get<SeedAdminCredentials>();

            if (adminSettings is null)
            {
                throw new InvalidOperationException("Required configuration section 'Identity:InitialSuperUser' is missing or could not be bound.");
            }

            var addSuperUser = services.GetRequiredService<IAddSuperUser>();

            Result addSuperUserResult = await addSuperUser.ExecuteAsync(adminSettings.Username, adminSettings.Password);

            if (!addSuperUserResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to seed initial super user '{adminSettings.Username}'. Error: {addSuperUserResult.Error}");
            }
        }
    }

    private sealed class SeedAdminCredentials
    {
        public required string Username  { get; init; }

        public required string Password { get; init; }
    }
}
