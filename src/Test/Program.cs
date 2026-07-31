using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Queries;
using Test.Repositories;

namespace Test;

class Program
{
    public async static Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ApplicationContext>(options => options.UseSqlite("Data Source=app.db"));

        services.AddEntityFrameworkUnits<ApplicationContext>();

        services.AddScoped<IAddUser, AddUserHandler>();

        services.AddScoped<IAddClaim, AddClaimHandler>();

        services.AddScoped<IGetUserId, GetUserIdHandler>();

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IClaimRepository, ClaimRepository>();

        services.AddScoped<ITestCommand, CommandOrchestrationTestHandler>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        await context.Database.MigrateAsync();

        var command = scope.ServiceProvider.GetRequiredService<ITestCommand>();

        await command.ExecuteAsync();
    }
}
