using System.ComponentModel.DataAnnotations;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore.Tests;

public sealed class TestEntity
{
    public int Id { get; init; }

    [MaxLength(10)]
    public string Name { get; init; } = null!;
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

[TestFixture]
public sealed class DbContextTransactionUnitTests : TestBase
{
    private SqliteConnection _connection = null!;

    private DbContextOptions<TestDbContext> _options = null!;

    private TestDbContext CreateDbContext() => new(_options);

    private readonly List<TestEntity> _expectedEntities = [];

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");

        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using TestDbContext context = CreateDbContext();

        await context.Database.EnsureCreatedAsync();

        _expectedEntities.Add(context.TestEntities.Add(new TestEntity { Name = "Test Entity Name 1" }).Entity);

        _expectedEntities.Add(context.TestEntities.Add(new TestEntity { Name = "Test Entity Name 2" }).Entity);

         await context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        _expectedEntities.Clear();

        await _connection.DisposeAsync();
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenUnsuccessful_ShouldRollbackTransactionAndNotPersistChanges()
    {
        // Arrange
        var testEntity = new TestEntity
        {
            Name = "Test Entity Name"
        };

        const string testErrorMessage = "Test Domain Error";

        Result result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                testContext.TestEntities.Add(testEntity);

                await transactionUnit.SaveChangesAsync(ct);

                return Result.DomainError(testErrorMessage);
            });
        }

        // Assert
        using (Assert.EnterMultipleScope())
        {
            await using TestDbContext assertionContext = CreateDbContext();

            AssertDomainError(result, testErrorMessage);

            Assert.That(assertionContext.TestEntities.ToList(), Is.EqualTo(_expectedEntities).UsingPropertiesComparer());
        }
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenSuccessful_ShouldCommitTransactionAndPersistChanges()
    {
        // Arrange
        var testEntity = new TestEntity
        {
            Name = "Test Entity Name"
        };

        _expectedEntities.Add(testEntity);

        Result result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                testContext.TestEntities.Add(testEntity);

                await transactionUnit.SaveChangesAsync(ct);

                return Result.Success();
            });
        }

        // Assert
        using (Assert.EnterMultipleScope())
        {
            await using TestDbContext assertionContext = CreateDbContext();

            AssertSuccess(result);

            Assert.That(assertionContext.TestEntities.ToList(), Is.EqualTo(_expectedEntities).UsingPropertiesComparer());
        }
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenExceptionOccurs_ShouldRollbackTransactionAndNotPersistChangesAndThrowException()
    {
        // Arrange
        var testEntity = new TestEntity
        {
            Name = "Test Entity Name"
        };

        const string testExceptionMessage = "Test Exception Message";

        Result? result = null;

        // Act
        Exception exception = Assert.ThrowsAsync<Exception>(async () =>
        {
            await using TestDbContext testContext = CreateDbContext();

            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            result = await transactionUnit.ExecuteInTransactionAsync<Result>(async ct =>
            {
                testContext.TestEntities.Add(testEntity);

                await transactionUnit.SaveChangesAsync(ct);

                throw new Exception(testExceptionMessage);
            });
        });

        // Assert
        using (Assert.EnterMultipleScope())
        {
            await using TestDbContext assertionContext = CreateDbContext();

            Assert.That(exception.Message, Is.EqualTo(testExceptionMessage));

            Assert.That(result, Is.Null);

            Assert.That(assertionContext.TestEntities.ToList(), Is.EqualTo(_expectedEntities).UsingPropertiesComparer());
        }
    }
}
