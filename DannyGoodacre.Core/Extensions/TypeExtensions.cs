using DannyGoodacre.Core.CommandQuery;

namespace DannyGoodacre.Core.Extensions;

internal static class TypeExtensions
{
    extension(Type type)
    {
        public bool IsCommandHandler()
        {
            var baseType = type.BaseType;

            while (baseType is not null)
            {
                if (baseType.IsGenericType)
                {
                    var definition = baseType.GetGenericTypeDefinition();

                    if (definition == typeof(CommandHandler<>)
                        || definition == typeof(CommandHandler<,>)
                        || definition == typeof(TransactionCommandHandler<>)
                        || definition == typeof(TransactionCommandHandler<,>))
                    {
                        return true;
                    }
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        public bool IsQueryHandler()
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

}
