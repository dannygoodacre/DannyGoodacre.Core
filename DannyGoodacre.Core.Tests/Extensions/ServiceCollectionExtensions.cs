using System.Reflection;
using DannyGoodacre.Core.CommandQuery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SystemMonitor.Core;

namespace DannyGoodacre.Core.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    private class MyCommand : ICommand;

    private interface ITestCommand;

    private class TestCommandHandler(ILogger logger) : CommandHandler<MyCommand>(logger), ITestCommand
    {
        protected override string CommandName => "Test Command";

        protected override Task<Result> InternalExecuteAsync(MyCommand command, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());
    }

    private interface ITestCommandWithValue;

    private class TestCommandWithValueHandler(ILogger logger) : CommandHandler<MyCommand, int>(logger), ITestCommandWithValue
    {
        protected override string CommandName => "Test Command";

        protected override Task<Result<int>> InternalExecuteAsync(MyCommand command, CancellationToken cancellationToken)
            => Task.FromResult(Result<int>.Success(123));
    }

    private class MyQuery : IQuery;

    private interface ITestQuery;

    private class TestQueryHandler(ILogger logger) : QueryHandler<MyQuery, int>(logger), ITestQuery
    {
        protected override string QueryName => "Test Query";

        protected override Task<Result<int>> InternalExecuteAsync(MyQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result<int>.Success(123));
    }

    [Test]
    public void AddCommandHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Mock.Of<ILogger>());

        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCommandHandlers(assembly);

        // Assert
        var provider = services.BuildServiceProvider();

        var testCommand = provider.GetService<ITestCommand>();

        Assert.That(testCommand, Is.Not.Null);

        var testCommandWithValue = provider.GetService<ITestCommand>();

        Assert.That(testCommandWithValue, Is.Not.Null);

        using var scope = provider.CreateScope();

        var instance1 = scope.ServiceProvider.GetRequiredService<ITestCommand>();
        var instance2 = scope.ServiceProvider.GetRequiredService<ITestCommand>();

        Assert.That(instance1, Is.SameAs(instance2));

        var instance3 = scope.ServiceProvider.GetRequiredService<ITestCommandWithValue>();
        var instance4 = scope.ServiceProvider.GetRequiredService<ITestCommandWithValue>();

        Assert.That(instance3, Is.SameAs(instance4));
    }

    [Test]
    public void AddQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Mock.Of<ILogger>());

        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddQueryHandlers(assembly);

        // Assert
        var provider = services.BuildServiceProvider();

        var registeredHandler = provider.GetService<ITestQuery>();

        Assert.That(registeredHandler, Is.Not.Null);

        using var scope = provider.CreateScope();

        var instance1 = scope.ServiceProvider.GetRequiredService<ITestQuery>();
        var instance2 = scope.ServiceProvider.GetRequiredService<ITestQuery>();

        Assert.That(instance1, Is.SameAs(instance2));
    }
}
