using DannyGoodacre.Cqrs.Examples.Auditing;
using DannyGoodacre.Cqrs.Examples.Listing;
using DannyGoodacre.Cqrs.Examples.Plans;
using DannyGoodacre.Cqrs.Examples.Validation;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs.Examples.Tests.InvalidOpenGenerics;

// Each type in this file makes AddCqrsHandlers throw at startup, because it could never run.
// OpenGenericRegistrationTests scans this assembly to show the error.

// A non-abstract open generic handler. The pipeline cannot be built for a request type that is not known.
public sealed class OpenListQueryHandler<TItem> : IQueryHandler<ListQuery<TItem>, IReadOnlyList<TItem>>
{
    public Task<IResult<IReadOnlyList<TItem>>> HandleAsync(
        ListQuery<TItem> query,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IResult<IReadOnlyList<TItem>>>(new Success<IReadOnlyList<TItem>>([]));
}

// Derived from the abstract generic base handler, but still open. Only a closed class, such as
// ListPlansQueryHandler, can be registered.
public sealed class DerivedOpenListQueryHandler<TItem> : ListQueryHandler<TItem>
{
    protected override Task<IReadOnlyList<TItem>> ReadPageAsync(int skip, int take, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TItem>>([]);
}

// A non-abstract open generic hook. The startup checks need the command type, to confirm that it is a state
// command with a matching handler.
public sealed class OpenAuditHook<TCommand> : IPostCommitHook<TCommand>
    where TCommand : ICommand
{
    public Task AfterCommitAsync(TCommand command, IResult result, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

// Derived from the abstract generic base hook, but still open. Only a closed class, such as
// RegisterCustomerAuditHook, can be registered.
public sealed class DerivedOpenAuditHook<TCommand, TResult>(ILogger<TCommand> logger)
    : AuditHook<TCommand, TResult>(logger)
    where TCommand : ICommand;

// The type parameter is inside the request type. The container closes a validator by passing the request type
// as its type argument, so it cannot work out TItem from ListQuery<TItem>. Only an abstract one, such as
// ListQueryValidator<TItem>, with closed validators derived from it, can be used.
public sealed class OpenListQueryValidator<TItem> : IValidator<ListQuery<TItem>>
{
    public void Validate(ListQuery<TItem> request, ValidationState validationState) { }
}

// The same shape, through the abstract generic base validator.
public sealed class DerivedListQueryValidator<TItem> : ValidatorBase<ListQuery<TItem>>
{
    public override void Validate(ListQuery<TItem> request, ValidationState validationState) { }
}

// The request type is closed, so the container has nothing to pass as TUnused.
public sealed class UnusedParameterValidator<TUnused> : IValidator<GetPlanQuery>
{
    public void Validate(GetPlanQuery request, ValidationState validationState) { }
}

// The container closes a validator with one type argument, the request type, so TOther cannot be filled.
public sealed class TwoParameterValidator<TRequest, TOther> : IValidator<TRequest>
    where TRequest : IRequest
{
    public void Validate(TRequest request, ValidationState validationState) { }
}
