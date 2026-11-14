using DannyGoodacre.Cqrs.Examples.Plans;

namespace DannyGoodacre.Cqrs.Examples.Tests.DuplicateRegistrations;

// Each type in this file makes AddCqrsHandlers throw at startup, because a request would have more than one handler,
// or a validator or hook would run twice. DuplicateRegistrationTests scans this assembly with the example project.
//
// A class derived from a concrete handler, validator, or hook is one too, so the base and the derived class are both
// registered. To keep one class that does everything, make the base abstract, as ValidatorBase<TRequest> and
// AuditHook<TCommand, TResult> are in the example project. To run two independent steps, remove the inheritance.

// A second handler for a request that GetPlanQueryHandler already handles. A request has exactly one handler, so
// which one ran would be arbitrary.
public sealed class CachedGetPlanQueryHandler : IQueryHandler<GetPlanQuery, PlanDetails>
{
    public Task<IResult<PlanDetails>> HandleAsync(GetPlanQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult<IResult<PlanDetails>>(new NotFound<PlanDetails>());
}

public sealed record ArchivePlanCommand(string PlanCode) : IStateCommand;

// A concrete base handler. The class derived from it also handles ArchivePlanCommand, so the request would have two.
public class ArchivePlanCommandHandler : ICommandHandler<ArchivePlanCommand>
{
    public virtual Task<IResult> HandleAsync(ArchivePlanCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IResult>(new Success());
}

public sealed class AuditedArchivePlanCommandHandler : ArchivePlanCommandHandler
{
    public override Task<IResult> HandleAsync(ArchivePlanCommand command, CancellationToken cancellationToken = default)
        => base.HandleAsync(command, cancellationToken);
}

// A concrete base validator. Both it and the validator derived from it would run, so a missing plan code would be
// reported twice.
public class ArchivePlanCommandValidator : IValidator<ArchivePlanCommand>
{
    public virtual void Validate(ArchivePlanCommand request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.PlanCode))
        {
            validationState.AddError(nameof(request.PlanCode), "Plan code is required.");
        }
    }
}

public sealed class StrictArchivePlanCommandValidator : ArchivePlanCommandValidator
{
    public override void Validate(ArchivePlanCommand request, ValidationState validationState)
    {
        base.Validate(request, validationState);

        if (request.PlanCode.Length > 20)
        {
            validationState.AddError(nameof(request.PlanCode), "Plan code cannot exceed 20 characters.");
        }
    }
}

// A concrete base hook. Both it and the hook derived from it would run, so the base's side effect would happen twice.
public class PlanArchivedHook : IPostCommitHook<ArchivePlanCommand>
{
    public virtual Task AfterCommitAsync(ArchivePlanCommand command, IResult result, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class PlanArchivedAndOwnerNotifiedHook : PlanArchivedHook
{
    public override Task AfterCommitAsync(ArchivePlanCommand command, IResult result, CancellationToken cancellationToken = default)
        => base.AfterCommitAsync(command, result, cancellationToken);
}
