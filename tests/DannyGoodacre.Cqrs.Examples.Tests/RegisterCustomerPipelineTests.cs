using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class RegisterCustomerPipelineTests
{
    private static Task<IResult<Guid>> SendAsync(ExamplesTestHost host, RegisterCustomerCommand command)
        => host.InScopeAsync(services => services
            .GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>()
            .HandleAsync(command, TestContext.Current.CancellationToken));

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_SavesCustomer()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        var command = new RegisterCustomerCommand("Jane Doe", "Jane@Example.com");

        // Act
        IResult<Guid> result = await SendAsync(host, command);

        // Assert
        Guid publicId = result.ShouldBeSuccess();

        Customer saved = await host.InScopeAsync(services => services
            .GetRequiredService<ExamplesDbContext>()
            .Customers
            .SingleAsync(c => c.PublicId == publicId, TestContext.Current.CancellationToken));

        // The database generated the primary key during the save.
        Assert.True(saved.Id > 0);
        Assert.Equal("Jane Doe", saved.Name);
        Assert.Equal("jane@example.com", saved.Email);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsInUse_ReturnsConflictAndDoesNotSave()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        (await SendAsync(host, new RegisterCustomerCommand("Jane Doe", "jane@example.com"))).ShouldBeSuccess();

        // Act
        IResult<Guid> result = await SendAsync(host, new RegisterCustomerCommand("Jane Smith", "JANE@example.com"));

        // Assert
        result.ShouldBeConflict("A customer with this email address already exists.");

        int customerCount = await host.InScopeAsync(services => services
            .GetRequiredService<ExamplesDbContext>()
            .Customers
            .CountAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, customerCount);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShortCircuitsBeforeTheDatabase()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        var command = new RegisterCustomerCommand("", "not-an-email");

        // Act
        IResult<Guid> result = await SendAsync(host, command);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal(2, validationState.Errors.Count);
        Assert.Contains(nameof(RegisterCustomerCommand.Name), validationState.Errors.Keys);
        Assert.Contains(nameof(RegisterCustomerCommand.Email), validationState.Errors.Keys);

        // The handler did not run, and the pipeline did not call SaveChangesAsync.
        Assert.Empty(host.DatabaseActivity.Commands);
    }
}
