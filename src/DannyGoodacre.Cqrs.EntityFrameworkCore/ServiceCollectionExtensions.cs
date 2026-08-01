using DannyGoodacre.Cqrs;
using DannyGoodacre.Cqrs.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Register <see cref="DbContextTransactionUnit{TDbContext}"/> and its corresponding
        /// <see cref="IStateUnit"/> and <see cref="ITransactionUnit"/> interfaces as scoped services.
        /// </summary>
        /// <typeparam name="TDbContext">The type of <see cref="DbContext"/>.</typeparam>
        /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
        public IServiceCollection AddEntityFrameworkUnits<TDbContext>()
            where TDbContext : DbContext
        {
            services.AddScoped<DbContextTransactionUnit<TDbContext>>();

            services.AddScoped<IStateUnit>(x => x.GetRequiredService<DbContextTransactionUnit<TDbContext>>());

            services.AddScoped<ITransactionUnit>(x => x.GetRequiredService<DbContextTransactionUnit<TDbContext>>());

            return services;
        }
    }
}
