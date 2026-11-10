using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Onboarding;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class OnboardCustomerPipelineTests
{
    private static async Task<(int Customers, int Subscriptions)> CountRowsAsync(ExamplesTestHost host)
    {
        await using AsyncServiceScope scope = host.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ExamplesDbContext>();

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        return (await dbContext.Customers.CountAsync(cancellationToken),
                await dbContext.Subscriptions.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenAllStepsSucceed_CommitsOneTransactionAndThenSendsEmail()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await using AsyncServiceScope scope = host.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ExamplesDbContext>();

        var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "PREMIUM");

        bool? transactionOpenWhenHookRan = null;

        host.EmailSender
            .Setup(x => x.SendWelcomeEmailAsync("jane@example.com", "Jane Doe", It.IsAny<CancellationToken>()))
            .Callback(() => transactionOpenWhenHookRan = dbContext.Database.CurrentTransaction is not null)
            .Returns(Task.CompletedTask);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>();

        // Act
        IResult<Guid> result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Guid publicId = result.ShouldBeSuccess();

        Assert.Equal((1, 1), await CountRowsAsync(host));

        // The subscription links to the primary key that the database generated in step 1.
        (int customerId, int subscriptionCustomerId) = await host.InScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ExamplesDbContext>();

            Customer customer = await db.Customers.SingleAsync(c => c.PublicId == publicId, TestContext.Current.CancellationToken);
            Subscription subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);

            return (customer.Id, subscription.CustomerId);
        });

        Assert.Equal(customerId, subscriptionCustomerId);

        // The child state command saved inside the outer transaction. It did not start a second one.
        Assert.Equal(1, host.DatabaseActivity.TransactionsStarted);
        Assert.Equal(1, host.DatabaseActivity.TransactionsCommitted);

        // The hook ran one time, after the commit.
        host.EmailSender.Verify(
            x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.False(transactionOpenWhenHookRan);

        CustomerDetails details = (await host.InScopeAsync(services => services
            .GetRequiredService<IQueryHandler<GetCustomerQuery, CustomerDetails>>()
            .HandleAsync(new GetCustomerQuery(publicId), TestContext.Current.CancellationToken)))
            .ShouldBeSuccess();

        Assert.Equal("PREMIUM", details.PlanCode);
    }

    [Fact]
    public async Task HandleAsync_WhenPlanDoesNotExist_RollsBackTheSavedCustomer()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        var command = new OnboardCustomerCommand("John Doe", "john@example.com", "GOLD");

        // Act
        IResult<Guid> result = await host.InScopeAsync(services => services
            .GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>()
            .HandleAsync(command, TestContext.Current.CancellationToken));

        // Assert
        result.ShouldBeNotFound();

        // Step 1 wrote the customer to the database inside the transaction...
        Assert.Contains(host.DatabaseActivity.Commands, sql => sql.Contains("INSERT INTO \"Customers\""));

        // ...and the rollback removed it.
        Assert.Equal(1, host.DatabaseActivity.TransactionsRolledBack);
        Assert.Equal(0, host.DatabaseActivity.TransactionsCommitted);
        Assert.Equal((0, 0), await CountRowsAsync(host));

        host.EmailSender.Verify(
            x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsInUse_ReturnsConflictFromTheChildCommand()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await host.InScopeAsync(services => services
            .GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>()
            .HandleAsync(new RegisterCustomerCommand("Jane Doe", "jane@example.com"), TestContext.Current.CancellationToken));

        var command = new OnboardCustomerCommand("Jane Smith", "jane@example.com", "BASIC");

        // Act
        IResult<Guid> result = await host.InScopeAsync(services => services
            .GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>()
            .HandleAsync(command, TestContext.Current.CancellationToken));

        // Assert
        result.ShouldBeConflict("A customer with this email address already exists.");

        Assert.Equal((1, 0), await CountRowsAsync(host));

        host.EmailSender.Verify(
            x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShortCircuitsBeforeTheTransaction()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        var command = new OnboardCustomerCommand("", "not-an-email", "");

        // Act
        IResult<Guid> result = await host.InScopeAsync(services => services
            .GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>()
            .HandleAsync(command, TestContext.Current.CancellationToken));

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal(3, validationState.Errors.Count);
        Assert.Contains(nameof(OnboardCustomerCommand.Name), validationState.Errors.Keys);
        Assert.Contains(nameof(OnboardCustomerCommand.Email), validationState.Errors.Keys);
        Assert.Contains(nameof(OnboardCustomerCommand.PlanCode), validationState.Errors.Keys);

        Assert.Equal(0, host.DatabaseActivity.TransactionsStarted);
        Assert.Empty(host.DatabaseActivity.Commands);

        host.EmailSender.Verify(
            x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("PREMIUM", true)]
    [InlineData("GOLD", false)]
    public async Task HandleAsync_WhenChildCommandHasHook_RunsItOnlyAfterTheOuterCommit(string planCode, bool expectHook)
    {
        // Arrange
        var childHookMock = new Mock<IPostCommitHook<RegisterCustomerCommand, Guid>>(MockBehavior.Strict);

        await using var host = await ExamplesTestHost.CreateAsync(services => services.AddSingleton(childHookMock.Object));

        await using AsyncServiceScope scope = host.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ExamplesDbContext>();

        bool? transactionOpenWhenChildHookRan = null;

        childHookMock
            .Setup(x => x.AfterCommitAsync(It.IsAny<RegisterCustomerCommand>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback(() => transactionOpenWhenChildHookRan = dbContext.Database.CurrentTransaction is not null)
            .Returns(Task.CompletedTask);

        host.EmailSender
            .Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<OnboardCustomerCommand, Guid>>();

        // Act
        IResult<Guid> result = await handler.HandleAsync(
            new OnboardCustomerCommand("Jane Doe", "jane@example.com", planCode),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectHook, result.IsSuccess);

        if (expectHook)
        {
            // The child hook waited for the outer transaction to commit.
            childHookMock.Verify(
                x => x.AfterCommitAsync(It.IsAny<RegisterCustomerCommand>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.False(transactionOpenWhenChildHookRan);
        }
        else
        {
            // The transaction rolled back the child's saved customer, so the child hook did not run.
            childHookMock.Verify(
                x => x.AfterCommitAsync(It.IsAny<RegisterCustomerCommand>(), It.IsAny<IResult<Guid>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
