using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class GetCustomerPipelineTests
{
    private static Task<IResult<CustomerDetails>> SendAsync(ExamplesTestHost host, GetCustomerQuery query)
        => host.InScopeAsync(services => services
            .GetRequiredService<IQueryHandler<GetCustomerQuery, CustomerDetails>>()
            .HandleAsync(query, TestContext.Current.CancellationToken));

    [Fact]
    public async Task HandleAsync_WhenCustomerExists_ReturnsDetailsWithPlan()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        var customer = new Customer { PublicId = Guid.NewGuid(), Name = "Jane Doe", Email = "jane@example.com" };

        await host.InScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ExamplesDbContext>();

            // Save the customer first, so the database generates customer.Id for the subscription.
            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            dbContext.Subscriptions.Add(new Subscription
            {
                PublicId = Guid.NewGuid(),
                CustomerId = customer.Id,
                PlanCode = "BASIC",
                StartedOn = new DateOnly(2026, 1, 1)
            });

            return await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act
        IResult<CustomerDetails> result = await SendAsync(host, new GetCustomerQuery(customer.PublicId));

        // Assert
        CustomerDetails details = result.ShouldBeSuccess();

        Assert.Equal(new CustomerDetails(customer.PublicId, "Jane Doe", "jane@example.com", "BASIC"), details);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<CustomerDetails> result = await SendAsync(host, new GetCustomerQuery(Guid.NewGuid()));

        // Assert
        result.ShouldBeNotFound();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicIdIsEmpty_ShortCircuitsBeforeTheDatabase()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<CustomerDetails> result = await SendAsync(host, new GetCustomerQuery(Guid.Empty));

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Contains(nameof(GetCustomerQuery.PublicId), validationState.Errors.Keys);
        Assert.Empty(host.DatabaseActivity.Commands);
    }
}
