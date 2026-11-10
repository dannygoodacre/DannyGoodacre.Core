using DannyGoodacre.Cqrs.Examples.Notifications;
using DannyGoodacre.Cqrs.Examples.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;

/// <summary>
/// Builds the example application with a private in-memory SQLite database for one test.
/// </summary>
internal sealed class ExamplesTestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private readonly ServiceProvider _provider;

    private ExamplesTestHost(
        SqliteConnection connection,
        ServiceProvider provider,
        Mock<IEmailSender> emailSender,
        DatabaseActivity databaseActivity)
    {
        _connection = connection;
        _provider = provider;
        EmailSender = emailSender;
        DatabaseActivity = databaseActivity;
    }

    /// <summary>
    /// A strict mock of the email sender. A hook failure does not change the command result,
    /// so a test that expects no email must verify it with <c>Times.Never</c>.
    /// </summary>
    public Mock<IEmailSender> EmailSender { get; }

    /// <summary>
    /// Records the SQL commands and transactions that the pipeline sends to the database.
    /// </summary>
    public DatabaseActivity DatabaseActivity { get; }

    public static async Task<ExamplesTestHost> CreateAsync(Action<IServiceCollection>? configureServices = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        var databaseActivity = new DatabaseActivity();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddCustomerOnboarding((_, options) => options
            .UseSqlite(connection)
            .AddInterceptors(databaseActivity));

        // The last registration wins, so the mock replaces the stub email sender.
        services.AddSingleton(emailSender.Object);

        configureServices?.Invoke(services);

        ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreatedAsync();
        }

        databaseActivity.Reset();

        return new ExamplesTestHost(connection, provider, emailSender, databaseActivity);
    }

    /// <summary>
    /// Creates a scope. Use one scope for each request, as a web application does.
    /// </summary>
    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public async Task<TResult> InScopeAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        await using AsyncServiceScope scope = CreateScope();

        return await action(scope.ServiceProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
