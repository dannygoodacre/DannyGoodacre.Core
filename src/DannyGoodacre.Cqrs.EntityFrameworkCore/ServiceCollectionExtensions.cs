using DannyGoodacre.Cqrs;
using DannyGoodacre.Cqrs.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkUnits<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<DbContextTransactionUnit<TDbContext>>();

        services.AddScoped<IStateUnit>(x => x.GetRequiredService<DbContextTransactionUnit<TDbContext>>());

        services.AddScoped<ITransactionUnit>(x => x.GetRequiredService<DbContextTransactionUnit<TDbContext>>());

        return services;
    }

}
