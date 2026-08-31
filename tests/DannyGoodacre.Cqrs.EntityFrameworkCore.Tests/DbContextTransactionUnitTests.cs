using System.ComponentModel.DataAnnotations;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
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
        _expectedEntities.Add(context.TestEntities.Add(new TestEntity { Name = "Test Entity Name 3" }).Entity);

        await context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        _expectedEntities.Clear();

        await _connection.DisposeAsync();
    }

    [Test]
    public async Task ExuteInTransactionAsync_WhenAlreadyInTransaction_ShouldNotStartNestedTransaction()
    {
        // Arrange
        var testEntity1 = new TestEntity
        {
            Name = "Test New Entity Name 1"
        };

        var testEntity2 = new TestEntity
        {
            Name = "Test New Entity Name 2"
        };

        _expectedEntities.Add(testEntity1);
        _expectedEntities.Add(testEntity2);

        IResult result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            await using IDbContextTransaction transaction = await testContext.Database.BeginTransactionAsync();

            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async cancellationToken =>
            {
                testContext.TestEntities.Add(testEntity1);
                testContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                return new Success();
            });

            await transaction.CommitAsync();
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
    public async Task ExecuteInTransactionAsync_WhenUnsuccessful_ShouldRollbackTransactionAndNotPersistChanges()
    {
        // Arrange
        var testEntity1 = new TestEntity
        {
            Name = "Test New Entity Name 1"
        };

        var testEntity2 = new TestEntity
        {
            Name = "Test New Entity Name 2"
        };

        const string testErrorMessage = "Test Domain Error";

        IResult result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async cancellationToken =>
            {
                testContext.TestEntities.Add(testEntity1);
                testContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                return new DomainError(testErrorMessage);
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
        var testEntity1 = new TestEntity
        {
            Name = "Test New Entity Name 1"
        };

        var testEntity2 = new TestEntity
        {
            Name = "Test New Entity Name 2"
        };

        _expectedEntities.Add(testEntity1);
        _expectedEntities.Add(testEntity2);

        const string testResponse = "Test Response";

        IResult<string> result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async cancellationToken =>
            {
                testContext.TestEntities.Add(testEntity1);
                testContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

                return new Success<string>(testResponse);
            });
        }

        // Assert
        using (Assert.EnterMultipleScope())
        {
            await using TestDbContext assertionContext = CreateDbContext();

            AssertSuccess(result, testResponse);

            Assert.That(assertionContext.TestEntities.ToList(), Is.EqualTo(_expectedEntities).UsingPropertiesComparer());
        }
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenExceptionOccurs_ShouldRollbackTransactionAndNotPersistChangesAndThrowException()
    {
        // Arrange
        var testEntity1 = new TestEntity
        {
            Name = "Test New Entity Name 1"
        };

        var testEntity2 = new TestEntity
        {
            Name = "Test New Entity Name 2"
        };

        const string testExceptionMessage = "Test Exception Message";

        IResult? result = null;

        // Act
        Exception exception = Assert.ThrowsAsync<Exception>(async () =>
        {
            await using TestDbContext testContext = CreateDbContext();

            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            result = await transactionUnit.ExecuteInTransactionAsync<IResult>(async cancellationToken =>
            {
                testContext.TestEntities.Add(testEntity1);
                testContext.TestEntities.Add(testEntity2);

                await transactionUnit.SaveChangesAsync(cancellationToken);

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
