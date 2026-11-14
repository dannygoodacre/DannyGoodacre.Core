using DannyGoodacre.Cqrs.Examples.Listing;
using DannyGoodacre.Cqrs.Examples.Persistence;

namespace DannyGoodacre.Cqrs.Examples.Plans;

// A closed validator derived from the abstract generic ListQueryValidator<TItem>, for ListQuery<PlanDetails> only.
public sealed class ListPlansQueryValidator : ListQueryValidator<PlanDetails>
{
    protected override int MaxPageSize => 10;
}

// A second closed handler derived from the abstract generic ListQueryHandler<TItem>, for ListQuery<PlanDetails>.
public sealed class ListPlansQueryHandler(IPlanRepository planRepository) : ListQueryHandler<PlanDetails>
{
    protected override Task<IReadOnlyList<PlanDetails>> ReadPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
        => planRepository.ListAsync(skip, take, cancellationToken);
}
