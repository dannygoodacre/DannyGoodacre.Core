namespace DannyGoodacre.Identity.Data;

internal interface IIdentityContextAccessor
{
    IdentityContext Context { get; }
}

internal sealed class IdentityContextAccessor<TContext>(TContext context) : IIdentityContextAccessor
    where TContext : IdentityContext
{
    public IdentityContext Context => context;
}
