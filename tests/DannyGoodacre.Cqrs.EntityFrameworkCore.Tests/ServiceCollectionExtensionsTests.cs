using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore.Tests;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEntityFrameworkUnits_RegistersUnitsAndPreservesScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEntityFrameworkUnits<TestDbContext>();

        using var provider = services.BuildServiceProvider();

        // Assert 1: Concrete implementation
        var concreteUnit = provider.GetService<DbContextTransaction<TestDbContext>>();
        Assert.NotNull(concreteUnit);

        // Assert 2: Interfaces resolvable
        var stateUnit = provider.GetService<IStateUnit>();
        Assert.NotNull(stateUnit);

        var transactionUnit = provider.GetService<ITransactionUnit>();
        Assert.NotNull(transactionUnit);

        // Assert 3: Same scope returns identical instances
        using (var scope1 = provider.CreateScope())
        {
            var stateUnit1 = scope1.ServiceProvider.GetRequiredService<IStateUnit>();
            var stateUnit2 = scope1.ServiceProvider.GetRequiredService<IStateUnit>();

            Assert.Same(stateUnit1, stateUnit2);

            var transactionUnit1 = scope1.ServiceProvider.GetRequiredService<ITransactionUnit>();
            var transactionUnit2 = scope1.ServiceProvider.GetRequiredService<ITransactionUnit>();

            Assert.Same(transactionUnit1, transactionUnit2);

            Assert.Same(stateUnit1, transactionUnit1);
        }

        // Assert 4: Different scopes return distinct instances
        using var scope2 = provider.CreateScope();
        using var scope3 = provider.CreateScope();

        var scope2StateUnit = scope2.ServiceProvider.GetRequiredService<IStateUnit>();
        var scope3StateUnit = scope3.ServiceProvider.GetRequiredService<IStateUnit>();

        Assert.NotSame(scope2StateUnit, scope3StateUnit);
    }
}
