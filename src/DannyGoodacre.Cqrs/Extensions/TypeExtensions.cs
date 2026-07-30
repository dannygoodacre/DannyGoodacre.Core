namespace DannyGoodacre.Cqrs;

internal static class TypeExtensions
{
    public static bool IsCommandHandler(this Type type)
    {
        Type? baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType.IsGenericType)
            {
                Type definition = baseType.GetGenericTypeDefinition();

                if (definition == typeof(CommandHandlerBase<,>))
                {
                    return true;
                }
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    public static bool IsQueryHandler(this Type type)
    {
        Type? baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType.IsGenericType)
            {
                Type definition = baseType.GetGenericTypeDefinition();

                if (definition == typeof(QueryHandler<,>))
                {
                    return true;
                }
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    internal static IEnumerable<Type> GetHandlerInterfaces(this Type type)
        => type.GetInterfaces()
            .Where(x => x != typeof(IDisposable) && x != typeof(IAsyncDisposable));

}
