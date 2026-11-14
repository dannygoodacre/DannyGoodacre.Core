using DannyGoodacre.Cqrs.Examples.Validation;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Listing;

// An open generic validator. AddCqrsHandlers registers it once, and the container closes it for every request that
// meets its constraints, such as ListQuery<PlanDetails> and SearchCustomersQuery. It derives from the abstract
// ValidatorBase<TRequest>, and its type parameter is still the request type, so it can be registered open.
public sealed class PagingValidator<TRequest> : ValidatorBase<TRequest>
    where TRequest : IRequest, IPagedRequest
{
    public const int MaxPageSize = 50;

    public override void Validate(TRequest request, ValidationState validationState)
    {
        AddErrorIf(validationState, request.Page < 1, nameof(IPagedRequest.Page), "Page must be at least 1.");

        AddErrorIf(
            validationState,
            request.PageSize is < 1 or > MaxPageSize,
            nameof(IPagedRequest.PageSize),
            $"Page size must be between 1 and {MaxPageSize}.");
    }
}

// An abstract generic validator whose type parameter is inside the request type. The container cannot work out TItem
// from ListQuery<PlanDetails>, so this validator cannot be registered open. It is abstract instead, and AddCqrsHandlers
// registers each closed validator derived from it, such as ListPlansQueryValidator, for its own list.
public abstract class ListQueryValidator<TItem> : ValidatorBase<ListQuery<TItem>>
{
    protected abstract int MaxPageSize { get; }

    public override void Validate(ListQuery<TItem> request, ValidationState validationState)
        => AddErrorIf(
            validationState,
            request.PageSize > MaxPageSize,
            nameof(request.PageSize),
            $"This list returns at most {MaxPageSize} items per page.");
}

// The constraints must all hold, so this validator runs only for requests that are both paged and searchable.
// ListQuery<TItem> is paged but not searchable, so it does not get this validator.
public sealed class SearchPagingValidator<TRequest> : IValidator<TRequest>
    where TRequest : IRequest, IPagedRequest, ISearchableRequest
{
    public const int MaxSearchPage = 5;

    public void Validate(TRequest request, ValidationState validationState)
    {
        if (request.Page > MaxSearchPage)
        {
            validationState.AddError(nameof(IPagedRequest.Page), $"Search results stop after page {MaxSearchPage}.");
        }
    }
}
