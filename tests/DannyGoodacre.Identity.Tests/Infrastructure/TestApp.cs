using System.Net;
using System.Net.Http.Json;
using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Tests.Infrastructure;

/// <summary>
/// An application on a test server, with its own SQLite database. Each test starts its own, so options can differ
/// per test.
/// </summary>
public sealed class TestApp : IAsyncDisposable
{
    public const string AdminUsername = "admin";

    public const string AdminPassword = "Admin-Password-1";

    public const string Password = "User-Password-1";

    private readonly WebApplication _app;

    private readonly SqliteConnection? _connection;

    private TestApp(WebApplication app, SqliteConnection? connection)
    {
        _app = app;
        _connection = connection;
    }

    public IServiceProvider Services => _app.Services;

    /// <summary>
    /// Checks the security stamp on every request, so admin, disabling, and password changes apply at once.
    /// </summary>
    public static void ValidateEveryRequest(IServiceCollection services)
        => services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

    /// <param name="configure">Configures the options after the test defaults.</param>
    /// <param name="map">
    /// Maps the endpoints. By default, all Identity endpoints, <c>GET /signed-in</c>, and <c>GET /admin-only</c>.
    /// </param>
    /// <param name="databasePath">A SQLite file to use instead of a private in-memory database.</param>
    /// <param name="initialize">Whether to run <c>InitializeIdentityAsync</c> before starting.</param>
    /// <param name="services">Registers extra services after the Identity services.</param>
    public static async Task<TestApp> StartAsync(
        Action<IdentityProviderOptions>? configure = null,
        Action<WebApplication>? map = null,
        string? databasePath = null,
        bool initialize = true,
        Action<IServiceCollection>? services = null)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = ["--hostBuilder:reloadConfigOnChange=false"]
        });

        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();

        SqliteConnection? connection = null;

        if (databasePath is null)
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            builder.Services.AddDbContext<TestIdentityContext>(options => options.UseSqlite(connection));
        }
        else
        {
            builder.Services.AddDbContext<TestIdentityContext>(options =>
                options.UseSqlite($"Data Source={databasePath};Pooling=False"));
        }

        builder.Services.AddUsernameIdentity<TestIdentityContext>(builder.Configuration, options =>
        {
            options.InitialAdmin = new InitialAdminOptions { Username = AdminUsername, Password = AdminPassword };
            configure?.Invoke(options);
        });

        builder.Services.AddDataProtection().PersistKeysToDbContext<TestIdentityContext>();

        services?.Invoke(builder.Services);

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        if (map is null)
        {
            app.MapIdentityEndpoints();
            app.MapGet("signed-in", () => Results.NoContent()).RequireAuthorization();
            app.MapGet("admin-only", () => Results.NoContent()).RequireAuthorization(IdentityDefaults.AdminPolicy);
        }
        else
        {
            map(app);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TestIdentityContext>().Database.EnsureCreatedAsync();
        }

        if (initialize)
        {
            await app.Services.InitializeIdentityAsync();
        }

        await app.StartAsync();

        return new TestApp(app, connection);
    }

    public HttpClient CreateClient(CookieContainer? cookies = null)
        => new(new CookieContainerHandler(cookies ?? new CookieContainer()) { InnerHandler = _app.GetTestServer().CreateHandler() })
        {
            BaseAddress = new Uri("https://localhost/")
        };

    public async Task<HttpClient> SignInAsync(string username, string password = Password, CookieContainer? cookies = null)
    {
        var client = CreateClient(cookies);

        var response = await client.PostAsJsonAsync(
            "sessions",
            new { username, password },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        return client;
    }

    public Task<HttpClient> SignInAsAdminAsync(CookieContainer? cookies = null)
        => SignInAsync(AdminUsername, AdminPassword, cookies);

    /// <summary>
    /// Registers through <c>POST /users</c> with the current invite code.
    /// </summary>
    public async Task<UserResponse> RegisterAsync(string username, string password = Password)
    {
        var response = await CreateClient().PostAsJsonAsync(
            "users",
            new { username, password, inviteCode = await GetInviteCodeAsync() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<UserResponse>(TestContext.Current.CancellationToken))!;
    }

    public async Task<Guid> CreateUserAsync(string username, bool admin = false, bool disabled = false)
        => await WithScopeAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<User>>();

            var user = new User { UserName = username };

            AssertSucceeded(await userManager.CreateAsync(user, Password));

            if (admin)
            {
                AssertSucceeded(await userManager.AddToRoleAsync(user, IdentityDefaults.AdminRole));
            }

            if (disabled)
            {
                AssertSucceeded(await userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            }

            return user.Id;
        });

    public Task<User?> FindUserAsync(string username)
        => WithScopeAsync(services => services.GetRequiredService<UserManager<User>>().FindByNameAsync(username));

    public Task<Guid> GetAdminIdAsync()
        => WithScopeAsync(async services =>
            (await services.GetRequiredService<UserManager<User>>().FindByNameAsync(AdminUsername))!.Id);

    public Task<Guid> GetInviteCodeAsync()
        => WithScopeAsync(async services =>
            (await services.GetRequiredService<TestIdentityContext>().Set<InviteCode>().SingleAsync()).Code);

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();

        return await action(scope.ServiceProvider);
    }

    public async Task WithScopeAsync(Func<IServiceProvider, Task> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();

        await action(scope.ServiceProvider);
    }

    public static void AssertSucceeded(IdentityResult result)
        => Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Code)));

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
