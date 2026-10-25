using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Repositories;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Identity.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity;

public static class ServiceProviderExtensions
{
    public async static Task SynchronizeIdentityPermissionsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        IServiceProvider services = scope.ServiceProvider;

        var claimRepository = services.GetRequiredService<IClaimRepository>();
        var userRepository = services.GetRequiredService<IUserRepository>();

        var stateUnit = services.GetRequiredService<IStateUnit>();

        var hashingService = services.GetRequiredService<IPasswordHashingService>();

        var permissionRegistry = services.GetRequiredService<IIdentityPermissionRegistry>();

        var config = services.GetRequiredService<IConfiguration>();

        List<Claim> existingClaims = await claimRepository.GetAllAsync(cancellationToken);

        HashSet<string> existingPermissionClaimValues = existingClaims
            .Where(x => x.Type == IdentityPermissionRegistry.PermissionClaimType)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> missingPermissions = permissionRegistry.AllPermissions
            .Where(permission => !existingPermissionClaimValues.Contains(permission))
            .ToList();

        if (missingPermissions.Count > 0)
        {
            foreach (string permission in missingPermissions)
            {
                claimRepository.Add(new Claim
                {
                    PublicId = Guid.NewGuid(),
                    Type = IdentityPermissionRegistry.PermissionClaimType,
                    Value = permission
                });
            }
        }

        SeedSuperUserCredentials? adminSettings = config
            .GetSection("Identity:InitialSuperUser")
            .Get<SeedSuperUserCredentials>();

        if (adminSettings is null)
        {
            throw new InvalidOperationException("Required configuration section 'Identity:InitialSuperUser' is missing or could not be bound.");
        }

        if (!await userRepository.ExistsAsync(adminSettings.Username, cancellationToken))
        {
            string passwordHash = hashingService.Hash(adminSettings.Password);

            User superUser = User.CreateNew(adminSettings.Username, passwordHash);

            superUser.IsApproved = true;

            userRepository.Add(superUser);
        }

        await stateUnit.SaveChangesAsync(cancellationToken);
    }

    private sealed class SeedSuperUserCredentials
    {
        public required string Username { get; init; }

        public required string Password { get; init; }
    }
}
