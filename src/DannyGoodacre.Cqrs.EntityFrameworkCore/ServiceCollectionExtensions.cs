using DannyGoodacre.Cqrs;
using DannyGoodacre.Cqrs.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="DbContextTransaction{TDbContext}"/> and forwards its
        /// <see cref="IStateUnit"/> and <see cref="ITransactionUnit"/> service resolution
        /// to the same scoped instance.
        /// </summary>
        /// <typeparam name="TDbContext">
        /// The type of <see cref="DbContext"/> managed by the transaction unit.
        /// </typeparam>
        /// <returns>
        /// The updated <see cref="IServiceCollection"/> instance for method chaining.
        /// </returns>
        public IServiceCollection AddEntityFrameworkUnits<TDbContext>()
            where TDbContext : DbContext
        {
            services.AddScoped<DbContextTransaction<TDbContext>>();

            services.AddScoped<IStateUnit>(
                sp => sp.GetRequiredService<DbContextTransaction<TDbContext>>());

            services.AddScoped<ITransactionUnit>(
                sp => sp.GetRequiredService<DbContextTransaction<TDbContext>>());

            return services;
        }
    }
}
