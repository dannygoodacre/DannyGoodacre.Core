using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity;

public sealed class IdentityBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    public IdentityBuilder RegisterPermissionsFromAssemblies(params Assembly[] assemblies)
    {
        var permissionTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: true, IsSealed: true }) // Static classes are abstract + sealed in C# IL
            .Where(t => t.GetCustomAttribute<PermissionsAttribute>() is not null);

        foreach (var type in permissionTypes)
        {
            Services.AddSingleton<IPermissionProvider>(new TypeReflectionPermissionProvider(type));
        }

        return this;
    }
}

/// <summary>
/// Marks a static class containing permission constant strings for automatic registry scanning.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PermissionsAttribute : Attribute;

internal sealed class TypeReflectionPermissionProvider(Type type) : IPermissionProvider
{
    public IReadOnlyCollection<string> GetPermissions()
        => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .ToList();
}

// // 1. Declare
// [Permissions]
// public static class BlackjackPermissions
// {
//     public const string TableCreate = "Blackjack.Tables.Create";
//     public const string TableDelete = "Blackjack.Tables.Delete";
// }
//
// // 2. Register (Program.cs)
// builder.Services
//     .AddIdentity<AppIdentityContext>(builder.Configuration)
//     .RegisterPermissionsFromAssemblies(typeof(BlackjackPermissions).Assembly);
