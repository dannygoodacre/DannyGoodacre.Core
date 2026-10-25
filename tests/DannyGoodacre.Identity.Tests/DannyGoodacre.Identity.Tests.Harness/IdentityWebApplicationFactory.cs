using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Tests.Harness;

public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            _connection = new SqliteConnection("DataSource=:memory:");

            _connection.Open();

            services.AddDbContext<TestIdentityContext>(x => x.UseSqlite(_connection));

            services.AddAuthorization();

            services.AddIdentity<TestIdentityContext>();

            services.AddScoped<ITransactionalUnitOfWork, TestTransactionalUnitOfWork>();

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<TestIdentityContext>();

            db.Database.EnsureCreated();
        });

        builder.Configure(app =>
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapIdentityEndpoints();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            var seedResult = app.SeedIdentityAsync("admin", "Password123$").GetAwaiter().GetResult();

            if (!seedResult.IsSuccess)
            {
                throw new InvalidOperationException($"Identity seeding failed: {seedResult.Error}");
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _connection?.Close();

        _connection?.Dispose();
    }
}
