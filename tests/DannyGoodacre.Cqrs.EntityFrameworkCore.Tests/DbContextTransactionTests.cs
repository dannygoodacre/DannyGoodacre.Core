using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore.Tests;

public sealed class TestEntity
{
    public int Id { get; init; }

    [MaxLength(10)]
    public string Name { get; init; } = null!;
}

public sealed class UnitTestDbContext(DbContextOptions<UnitTestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

public sealed class DbContextTransactionTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    private DbContextOptions<UnitTestDbContext> _options = null!;

    private readonly List<TestEntity> _expectedEntities = [];

    private UnitTestDbContext CreateDbContext() => new(_options);

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");

        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<UnitTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using UnitTestDbContext context = CreateDbContext();

        await context.Database.EnsureCreatedAsync();

        _expectedEntities.Add(context.TestEntities.Add(new TestEntity { Name = "Test Entity 1" }).Entity);
        _expectedEntities.Add(context.TestEntities.Add(new TestEntity { Name = "Test Entity 2" }).Entity);
        _expectedEntities.Add(context.TestEntities.Add(new TestEntity { Name = "Test Entity 3" }).Entity);

        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _expectedEntities.Clear();

        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsPendingChangesToDatabase()
    {
        // Arrange
        await using var context = CreateDbContext();

        var transactionUnit = new DbContextTransaction<UnitTestDbContext>(context);

        var newEntity = new TestEntity { Name = "Entity 4" };

        context.TestEntities.Add(newEntity);

        // Act
        int savedChanges = await transactionUnit.SaveChangesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, savedChanges);

        await using var assertionContext = CreateDbContext();

        Assert.Contains(assertionContext.TestEntities, e => e.Name == "Entity 4");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenAlreadyInTransaction_DoesNotStartNestedTransaction()
    {
        // Arrange
        var testEntity1 = new TestEntity { Name = "New Entity 1" };
        var testEntity2 = new TestEntity { Name = "New Entity 2" };

        _expectedEntities.Add(testEntity1);
        _expectedEntities.Add(testEntity2);

        IResult result;

        await using (UnitTestDbContext unitTestContext = CreateDbContext())
        {
            await using IDbContextTransaction transaction = await unitTestContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

            var transactionUnit = new DbContextTransaction<UnitTestDbContext>(unitTestContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async cancellationToken =>
            {
                unitTestContext.TestEntities.Add(testEntity1);
                unitTestContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }, TestContext.Current.CancellationToken);

            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        result.ShouldBeSuccess();

        await using UnitTestDbContext assertionContext = CreateDbContext();

        Assert.Equal(_expectedEntities.Select(e => e.Name), assertionContext.TestEntities.Select(e => e.Name));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenSuccessful_CommitsTransactionAndPersistsChanges()
    {
        // Arrange
        var testEntity1 = new TestEntity { Name = "New Entity 1" };
        var testEntity2 = new TestEntity { Name = "New Entity 2" };

        _expectedEntities.Add(testEntity1);
        _expectedEntities.Add(testEntity2);

        const string testResponse = "Test Response";

        IResult<string> result;

        await using (UnitTestDbContext unitTestContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransaction<UnitTestDbContext>(unitTestContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async cancellationToken =>
            {
                unitTestContext.TestEntities.Add(testEntity1);
                unitTestContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                return Result<string>.Success(testResponse);
            }, TestContext.Current.CancellationToken);
        }

        // Assert
        result.ShouldBeSuccess(testResponse);

        await using UnitTestDbContext assertionContext = CreateDbContext();

        Assert.Equal(_expectedEntities.Select(e => e.Name), assertionContext.TestEntities.Select(e => e.Name));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenUnsuccessful_RollsBackTransactionClearsChangeTrackerAndDoesNotPersistChanges()
    {
        // Arrange
        var testEntity1 = new TestEntity { Name = "New Entity 1" };
        var testEntity2 = new TestEntity { Name = "New Entity 2" };

        const string testErrorMessage = "Test Domain Error";

        IResult result;

        await using (UnitTestDbContext unitTestContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransaction<UnitTestDbContext>(unitTestContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async cancellationToken =>
            {
                unitTestContext.TestEntities.Add(testEntity1);
                unitTestContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                return Result.DomainError(testErrorMessage);
            }, TestContext.Current.CancellationToken);

            // Assert ChangeTracker is cleared on failure
            Assert.Empty(unitTestContext.ChangeTracker.Entries());
        }

        // Assert
        result.ShouldBeDomainError(testErrorMessage);

        await using UnitTestDbContext assertionContext = CreateDbContext();

        Assert.Equal(_expectedEntities.Select(e => e.Name), assertionContext.TestEntities.Select(e => e.Name));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenExceptionOccurs_RollsBackTransactionClearsChangeTrackerAndRethrowsException()
    {
        // Arrange
        var testEntity1 = new TestEntity { Name = "New Entity 1" };
        var testEntity2 = new TestEntity { Name = "New Entity 2" };

        const string testExceptionMessage = "Test Exception Message";

        await using UnitTestDbContext unitTestContext = CreateDbContext();

        var transactionUnit = new DbContextTransaction<UnitTestDbContext>(unitTestContext);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await transactionUnit.ExecuteInTransactionAsync<IResult>(async cancellationToken =>
            {
                unitTestContext.TestEntities.Add(testEntity1);
                unitTestContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException(testExceptionMessage);
            }, TestContext.Current.CancellationToken);
        });

        Assert.Equal(testExceptionMessage, exception.Message);

        // Assert ChangeTracker is cleared on exception
        Assert.Empty(unitTestContext.ChangeTracker.Entries());

        await using UnitTestDbContext assertionContext = CreateDbContext();

        Assert.Equal(_expectedEntities.Select(e => e.Name), assertionContext.TestEntities.Select(e => e.Name));
    }
}
