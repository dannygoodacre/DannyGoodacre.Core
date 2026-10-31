using Microsoft.Extensions.Logging.Testing;

namespace DannyGoodacre.Cqrs.Testing;

/// <summary>
/// Base test context supplying an in-memory <see cref="FakeLogger"/>
/// and <see cref="FakeLogCollector"/> for a specified logging category.
/// </summary>
public class HandlerTestContext<TCategory>
    where TCategory : class
{
    public FakeLogCollector LogCollector { get; } = new();

    public FakeLogger<TCategory> Logger { get; }

    public HandlerTestContext()
    {
        Logger = new FakeLogger<TCategory>(LogCollector);
    }
}
