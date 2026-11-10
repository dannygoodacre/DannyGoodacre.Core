using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Plans;

public sealed record PlanDetails(string Code, string Name, decimal MonthlyPrice);

public sealed record GetPlanQuery(string PlanCode) : IQuery<PlanDetails>;

public sealed class GetPlanQueryValidator : IValidator<GetPlanQuery>
{
    public void Validate(GetPlanQuery request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.PlanCode))
        {
            validationState.AddError(nameof(request.PlanCode), "Plan code is required.");
        }
    }
}

public sealed class GetPlanQueryHandler(IPlanRepository planRepository)
    : IQueryHandler<GetPlanQuery, PlanDetails>
{
    public async Task<IResult<PlanDetails>> HandleAsync(
        GetPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        PlanDetails? plan = await planRepository.FindAsync(query.PlanCode, cancellationToken);

        return plan is null
            ? new NotFound<PlanDetails>()
            : new Success<PlanDetails>(plan);
    }
}
