using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore.Tests;

public sealed class TestDbContextFoo(DbContextOptions<TestDbContextFoo> options) : DbContext(options);

[TestFixture]
public sealed class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddEntityFrameworkUnits()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContextFoo>(options => options.UseInMemoryDatabase("TestDb"));

        // Act
        services.AddEntityFrameworkUnits<TestDbContextFoo>();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();

        DbContextTransactionUnit<TestDbContextFoo>? dbContext = provider.GetService<DbContextTransactionUnit<TestDbContextFoo>>();

        Assert.That(dbContext, Is.Not.Null);

        IStateUnit? stateUnit = provider.GetService<IStateUnit>();

        Assert.That(stateUnit, Is.Not.Null);

        ITransactionUnit? transactionUnit = provider.GetService<ITransactionUnit>();

        Assert.That(transactionUnit, Is.Not.Null);

        using IServiceScope scope = provider.CreateScope();

        IStateUnit stateUnit1 = scope.ServiceProvider.GetRequiredService<IStateUnit>();
        IStateUnit stateUnit2 = scope.ServiceProvider.GetRequiredService<IStateUnit>();

        Assert.That(stateUnit1, Is.SameAs(stateUnit2));

        ITransactionUnit transactionUnit1 = scope.ServiceProvider.GetRequiredService<ITransactionUnit>();
        ITransactionUnit transactionUnit2 = scope.ServiceProvider.GetRequiredService<ITransactionUnit>();

        Assert.That(transactionUnit1, Is.SameAs(transactionUnit2));
    }
}
