using Microsoft.Extensions.Logging.Testing;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Base test context supplying an in-memory <see cref="FakeLogger"/>
/// and <see cref="FakeLogCollector"/> for a specified logging category.
/// </summary>
/// <typeparam name="TCategory">The logging category, normally the request type under test.</typeparam>
public class TestContext<TCategory> where TCategory : class
{
    /// <summary>
    /// The collector that records every entry written to <see cref="Logger"/>.
    /// </summary>
    public FakeLogCollector LogCollector { get; } = new();

    /// <summary>
    /// The logger passed to the decorator under test. Its entries are recorded in <see cref="LogCollector"/>.
    /// </summary>
    public FakeLogger<TCategory> Logger { get; }

    /// <summary>
    /// Creates a context with an empty <see cref="LogCollector"/> and a <see cref="Logger"/> that writes to it.
    /// </summary>
    public TestContext()
    {
        Logger = new FakeLogger<TCategory>(LogCollector);
    }
}
