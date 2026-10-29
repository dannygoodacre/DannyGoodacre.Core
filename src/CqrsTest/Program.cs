using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CqrsTest;

class Program
{
    async static Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("  CQRS Automated Registration & Decorator Test");
        Console.WriteLine("==================================================\n");

        // 1. Setup Dependency Injection Container
        var services = new ServiceCollection();

        // Add Console Logging
        services.AddLogging(builder => builder.AddConsole());

        services.AddScoped<IStateUnit, StateUnit>();

        services.AddScoped<IUserRepository, UserRepository>();

        // Scan assemblies and register handlers with decorators automatically
        services.AddCommandHandlers(typeof(Program).Assembly);

        var serviceProvider = services.BuildServiceProvider();

        // 2. Create Scope and Resolve Handler
        using var scope = serviceProvider.CreateScope();

        Console.WriteLine("Resolving ICommandHandler<CreateUserCommand, Guid>...");

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateUserCommand>>();

        Console.WriteLine($"Resolved Handler Type: {handler.GetType().Name}\n");

        // 3. Execute Command
        var command = new CreateUserCommand("", "");

        var result = await handler.HandleAsync(command);

        // if (result is Success<Guid> successResult)
        // {
        //     Console.WriteLine($"\n[SUCCESS] Created User Public ID: {successResult.Value}");
        // }

        if (result is Success)
        {
            Console.WriteLine($"\n[SUCCESS] Created user with void command");
        }
    }
}
