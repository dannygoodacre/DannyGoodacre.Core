namespace DannyGoodacre.Cqrs.Tests;

public sealed class TestContextMockBehaviorTests
{
    public sealed class Command : ICommand;

    public sealed class Query : IQuery<string>;

    [Theory]
    [InlineData(MockBehavior.Loose)]
    [InlineData(MockBehavior.Strict)]
    public void Constructor_UsesTheGivenMockBehaviorForEveryMock(MockBehavior mockBehavior)
    {
        // Act
        Mock[] mocks =
        [
            .. MocksOf(new CommandValidationTestContext<Command>(mockBehavior)),
            .. MocksOf(new CommandValidationTestContext<Command, string>(mockBehavior)),
            .. MocksOf(new QueryValidationTestContext<Query, string>(mockBehavior)),
            .. MocksOf(new StateCommandTestContext<Command>(mockBehavior)),
            .. MocksOf(new StateCommandTestContext<Command, string>(mockBehavior)),
            .. MocksOf(new TransactedStateCommandTestContext<Command>(mockBehavior)),
            .. MocksOf(new TransactedStateCommandTestContext<Command, string>(mockBehavior))
        ];

        // Assert
        Assert.All(mocks, mock => Assert.Equal(mockBehavior, mock.Behavior));
    }

    // Finds every public Mock property, so a mock added to a context later is checked too.
    private static IEnumerable<Mock> MocksOf(object context)
        => context.GetType()
            .GetProperties()
            .Where(p => typeof(Mock).IsAssignableFrom(p.PropertyType))
            .Select(p => (Mock)p.GetValue(context)!);
}
