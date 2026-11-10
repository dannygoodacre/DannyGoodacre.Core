using DannyGoodacre.Cqrs;
using DannyGoodacre.Cqrs.Examples;
using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Onboarding;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Primitives;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class Program
{
    public async static Task Main()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_ =>
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        });

        services.AddLogging(logging => logging
            .AddSimpleConsole(options => options.SingleLine = true)
            .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));

        services.AddCustomerOnboarding((provider, options) =>
            options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));

        await using ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreatedAsync();

        var register = scope.ServiceProvider.GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>();
        var onboard = scope.ServiceProvider.GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>();
        var getCustomer = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetCustomerQuery, CustomerDetails>>();

        List<Guid> customerIds = [];

        // A state command. The pipeline saves the customer after the handler succeeds.
        IResult<Guid> registered = await register.HandleAsync(new RegisterCustomerCommand("Ann Lee", "ann@example.com"));

        if (registered is Success<Guid>(var registeredId))
        {
            Console.WriteLine($"Registered customer {registeredId}.");

            customerIds.Add(registeredId);
        }

        // A transacted command. It registers a customer and subscribes them to a plan in one transaction.
        OnboardCustomerCommand[] onboardings =
        [
            new("Jane Doe", "jane@example.com", "PREMIUM"), // Success: one transaction saves the customer and the subscription.
            new("Jane Smith", "JANE@example.com", "BASIC"), // Conflict: the child command finds the email in use.
            new("John Doe", "john@example.com", "GOLD"),    // NotFound: the transaction rolls back the customer that step 1 saved.
            new("", "not-an-email", "")                     // Invalid: the validator stops the request before the transaction starts.
        ];

        foreach (OnboardCustomerCommand command in onboardings)
        {
            IResult<Guid> result = await onboard.HandleAsync(command);

            Console.WriteLine(result switch
            {
                Success<Guid>(var customerId) => $"Onboarded customer {customerId} on plan {command.PlanCode}.",
                Invalid(var validationState) => $"The request is not valid:{Environment.NewLine}{validationState}",
                Conflict(var message) => $"Conflict: {message}",
                NotFound => $"Plan '{command.PlanCode}' does not exist. No data was saved.",
                _ => $"The request failed: {result}"
            });

            if (result is Success<Guid>(var id))
            {
                customerIds.Add(id);
            }
        }

        // A query. The last ID does not exist.
        foreach (Guid customerId in customerIds.Append(Guid.NewGuid()))
        {
            IResult<CustomerDetails> result = await getCustomer.HandleAsync(new GetCustomerQuery(customerId));

            Console.WriteLine(result switch
            {
                Success<CustomerDetails>(var customer) =>
                    $"Customer {customer.PublicId} is on plan {customer.PlanCode ?? "(none)"}.",
                NotFound => $"Customer {customerId} does not exist.",
                _ => $"The query failed: {result}"
            });
        }
    }
}
