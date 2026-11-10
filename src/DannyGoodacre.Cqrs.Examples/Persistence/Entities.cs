// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DannyGoodacre.Cqrs.Examples.Persistence;

public sealed class Customer
{
    public int Id { get; init; }

    public required Guid PublicId { get; init; }

    public required string Name { get; init; } = null!;

    public required string Email { get; init; } = null!;
}

public sealed class Plan
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required decimal MonthlyPrice { get; init; }
}

public sealed class Subscription
{
    public int Id { get; init; }

    public required Guid PublicId { get; init; }

    public required int CustomerId { get; init; }

    public required string PlanCode { get; init; }

    public required DateOnly StartedOn { get; init; }
}
