using DannyGoodacre.Results;

namespace DannyGoodacre.Cqrs.Examples.Validation;

// A base class for validators. It is abstract, so AddCqrsHandlers does not register it. It registers each validator
// derived from it: a closed one, such as SearchCustomersQueryValidator, for its request, and an open generic one,
// such as PagingValidator<TRequest>, for every request that meets its constraints.
public abstract class ValidatorBase<TRequest> : IValidator<TRequest>
    where TRequest : IRequest
{
    public abstract void Validate(TRequest request, ValidationState validationState);
}
