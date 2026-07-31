using DannyGoodacre.Primitives;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Cqrs.EntityFrameworkCore.Tests;

public class TestEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

[TestFixture]
public class DbContextTransactionUnitTest
{
    private SqliteConnection _connection = null!;

    private DbContextOptions<TestDbContext> _options = null!;

    private TestDbContext CreateDbContext() => new(_options);

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
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenUnsuccessful_ShouldRollbackTransactionAndNotPersistChanges()
    {
        // Arrange
        Result result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                testContext.TestEntities.Add(new TestEntity { Name = "Test Name" });

                await transactionUnit.SaveChangesAsync(ct);

                return Result.DomainError("Test Domain Error");
            });
        }

        // Assert
        Assert.That(result.IsSuccess, Is.False);

        await using (TestDbContext assertionContext = CreateDbContext())
        {
            int recordCount = await assertionContext.TestEntities.CountAsync();

            Assert.That(recordCount, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenSuccessful_ShouldCommitTransactionAndPersistChanges()
    {
        // Arrange
        Result result;

        await using (TestDbContext testContext = CreateDbContext())
        {
            var transactionUnit = new DbContextTransactionUnit<TestDbContext>(testContext);

            // Act
            result = await transactionUnit.ExecuteInTransactionAsync(async ct =>
            {
                testContext.TestEntities.Add(new TestEntity { Name = "Test Name" });

                await transactionUnit.SaveChangesAsync(ct);

                return Result.Success();
            });
        }

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        await using (TestDbContext assertionContext = CreateDbContext())
        {
            int recordCount = await assertionContext.TestEntities.CountAsync();

            Assert.That(recordCount, Is.EqualTo(1));

            TestEntity? testEntity = await assertionContext.TestEntities.SingleOrDefaultAsync();

            Assert.That(testEntity, Is.Not.Null);

            Assert.That(testEntity.Name, Is.EqualTo("Test Name"));
        }
    }
}
