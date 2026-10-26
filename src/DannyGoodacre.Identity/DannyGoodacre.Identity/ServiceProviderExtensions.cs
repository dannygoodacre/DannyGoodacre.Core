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
    public async static Task SynchronizeIdentityPermissionsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        var claimRepository = services.GetRequiredService<IClaimRepository>();
        var roleRepository = services.GetRequiredService<IRoleRepository>();
        var userRepository = services.GetRequiredService<IUserRepository>();
        var stateUnit = services.GetRequiredService<IStateUnit>();
        var hashingService = services.GetRequiredService<IPasswordHashingService>();
        var permissionRegistry = services.GetRequiredService<IIdentityPermissionRegistry>();
        var roleRegistries = services.GetServices<IRolePermissionRegistry>();
        var config = services.GetRequiredService<IConfiguration>();

        // -------------------------------------------------------------------------
        // 1. Synchronize & Soft-Deprecate Permission Claims
        // -------------------------------------------------------------------------
        List<Claim> existingClaims = await claimRepository.GetAllAsync(cancellationToken);

        HashSet<string> registeredPermissions = permissionRegistry.AllPermissions
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Add missing permissions from code into the database
        foreach (string permission in registeredPermissions)
        {
            if (!existingClaims.Any(x => x.Type == IdentityPermissionRegistry.PermissionClaimType
                                         && string.Equals(x.Value, permission, StringComparison.OrdinalIgnoreCase)))
            {
                var newClaim = new Claim
                {
                    PublicId = Guid.NewGuid(),
                    Type = IdentityPermissionRegistry.PermissionClaimType,
                    Value = permission,
                    IsDisabled = false
                };

                claimRepository.Add(newClaim);
                existingClaims.Add(newClaim);
            }
        }

        // Soft-deprecate claims present in DB but removed from code
        foreach (Claim claim in existingClaims.Where(x => x.Type == IdentityPermissionRegistry.PermissionClaimType))
        {
            claim.IsDisabled = !registeredPermissions.Contains(claim.Value);
        }

        // Save new claims so EF assigns primary keys prior to relational mapping
        await stateUnit.SaveChangesAsync(cancellationToken);

        Dictionary<string, Claim> claimsByValue = existingClaims
            .Where(x => x.Type == IdentityPermissionRegistry.PermissionClaimType && !x.IsDisabled)
            .ToDictionary(x => x.Value, StringComparer.OrdinalIgnoreCase);

        // -------------------------------------------------------------------------
        // 2. Synchronize & Soft-Deprecate Roles & Role-Permission Mappings
        // -------------------------------------------------------------------------
        Dictionary<string, List<string>> codeRoleMappings = roleRegistries
            .SelectMany(r => r.RoleMappings)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        List<Role> existingRoles = await roleRepository.GetAllAsync(cancellationToken);

        // Add or update active roles from code
        foreach (var (roleName, permissions) in codeRoleMappings)
        {
            Role? role = existingRoles.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));

            if (role is null)
            {
                role = new Role
                {
                    PublicId = Guid.NewGuid(),
                    Name = roleName,
                    IsDisabled = false
                };
                roleRepository.Add(role);
                existingRoles.Add(role);
            }
            else
            {
                role.IsDisabled = false;
            }

            // Track existing claim IDs in memory to ensure duplicate join entities are never added
            HashSet<int> existingRoleClaimIds = role.Claims.Select(rc => rc.ClaimId).ToHashSet();

            // Sync active role-permission links
            foreach (string permission in permissions)
            {
                if (claimsByValue.TryGetValue(permission, out Claim? claim))
                {
                    if (existingRoleClaimIds.Add(claim.Id))
                    {
                        role.Claims.Add(new RoleClaim { RoleId = role.Id, ClaimId = claim.Id });
                    }
                }
            }
        }

        // Soft-deprecate roles present in DB but removed from code mappings
        foreach (Role role in existingRoles)
        {
            if (!codeRoleMappings.ContainsKey(role.Name))
            {
                role.IsDisabled = true;
            }
        }

        // -------------------------------------------------------------------------
        // 3. Seed Initial SuperUser & Assign All Active Discovered Permissions
        // -------------------------------------------------------------------------
        SeedSuperUserCredentials? adminSettings = config
            .GetSection("Identity:InitialSuperUser")
            .Get<SeedSuperUserCredentials>();

        if (adminSettings is null)
        {
            throw new InvalidOperationException("Required configuration section 'Identity:InitialSuperUser' is missing or could not be bound.");
        }

        User? superUser = await userRepository.GetAsync(adminSettings.Username, cancellationToken);

        if (superUser is null)
        {
            string passwordHash = hashingService.Hash(adminSettings.Password);

            superUser = User.CreateNew(adminSettings.Username, passwordHash);
            superUser.IsApproved = true;

            userRepository.Add(superUser);
        }

        // Track existing claim IDs in memory to ensure duplicate join entities are never added
        HashSet<int> existingUserClaimIds = superUser.Claims.Select(uc => uc.ClaimId).ToHashSet();

        // Attach all active, non-disabled permission claims directly to SuperUser
        foreach (Claim activeClaim in claimsByValue.Values)
        {
            if (existingUserClaimIds.Add(activeClaim.Id))
            {
                superUser.Claims.Add(new UserClaim { UserId = superUser.Id, ClaimId = activeClaim.Id });
            }
        }

        // -------------------------------------------------------------------------
        // 4. Atomic Commit
        // -------------------------------------------------------------------------
        await stateUnit.SaveChangesAsync(cancellationToken);
    }

    private sealed class SeedSuperUserCredentials
    {
        public required string Username { get; init; }

        public required string Password { get; init; }
    }
}
