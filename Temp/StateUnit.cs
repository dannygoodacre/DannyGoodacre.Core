using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Temp;

public class StateUnit<TContext>(TContext context) : IStateUnit
    where TContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
