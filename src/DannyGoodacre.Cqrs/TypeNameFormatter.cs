namespace DannyGoodacre.Cqrs;

// For log messages and startup errors: ListQuery<PlanDetails> rather than ListQuery`1.
internal static class TypeNameFormatter
{
    public static string Format(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        // A type nested in a generic type is generic without declaring its own type parameters, so its name has no `.
        int arityIndex = type.Name.IndexOf('`');

        string name = arityIndex < 0
            ? type.Name
            : type.Name[..arityIndex];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Format))}>";
    }
}
