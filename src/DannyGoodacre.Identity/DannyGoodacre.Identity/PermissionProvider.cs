using System.Reflection;

namespace DannyGoodacre.Identity;

internal interface IPermissionProvider
{
    IReadOnlyCollection<string> GetPermissions();
}

internal sealed class TypeReflectionPermissionProvider(Type type) : IPermissionProvider
{
    public IReadOnlyCollection<string> GetPermissions()
        => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .ToList();
}
