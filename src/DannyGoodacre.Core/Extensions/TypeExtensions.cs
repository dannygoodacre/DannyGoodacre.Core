using DannyGoodacre.Core.CommandQuery;

namespace DannyGoodacre.Core.Extensions;

internal static class TypeExtensions
{
    extension(Type type)
    {
        public bool IsCommandHandler()
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

        public bool IsQueryHandler()
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

        public IEnumerable<Type> GetHandlerInterfaces()
            => type.GetInterfaces()
                .Where(x => x != typeof(IDisposable) && x != typeof(IAsyncDisposable));
    }
}
