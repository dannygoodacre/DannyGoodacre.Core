using DannyGoodacre.Core.CommandQuery;

namespace DannyGoodacre.Core.Extensions;

internal static class TypeExtensions
{
    public static bool IsCommandHandler(this Type type)
    {
        var baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType.IsGenericType)
            {
                var definition = baseType.GetGenericTypeDefinition();

                if (definition == typeof(CommandHandler<>)
                    || definition == typeof(CommandHandler<,>)
                    || definition == typeof(UnitOfWorkCommandHandler<>)
                    || definition == typeof(UnitOfWorkCommandHandler<,>))
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
        var baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType.IsGenericType)
            {
                var definition = baseType.GetGenericTypeDefinition();

                if (definition == typeof(QueryHandler<,>))
                {
                    return true;
                }
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}
