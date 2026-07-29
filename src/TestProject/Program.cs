using DannyGoodacre.Cqrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestProject.Queries;
using TestProject.Repositories;

namespace TestProject;

public class Program
{
    public async static Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<IdentityContext>(options => options.UseSqlite("Data Source=identity.db"));

        services.AddScoped<IStateUnit>(x => x.GetRequiredService<IdentityContext>());

        services.AddScoped<ITransactionUnit>(x => x.GetRequiredService<IdentityContext>());

        services.AddScoped<IAddUser, AddUserHandler>();

        services.AddScoped<IAddClaim, AddClaimHandler>();

        services.AddScoped<IGetUserId, GetUserIdHandler>();

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IClaimRepository, ClaimRepository>();

        services.AddScoped<ITestCommand, CommandOrchestrationTestHandler>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityContext>();

        await dbContext.Database.MigrateAsync();

        var command = scope.ServiceProvider.GetRequiredService<ITestCommand>();

        await command.ExecuteAsync();
    }
}
