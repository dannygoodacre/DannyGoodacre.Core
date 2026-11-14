using DannyGoodacre.Cqrs.Examples.Listing;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Cqrs.Examples.Validation;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Customers;

// A closed handler derived from the abstract generic ListQueryHandler<TItem>. AddCqrsHandlers registers it
// as the handler of ListQuery<CustomerDetails>.
public sealed class ListCustomersQueryHandler(ICustomerRepository customerRepository)
    : ListQueryHandler<CustomerDetails>
{
    protected override Task<IReadOnlyList<CustomerDetails>> ReadPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
        => customerRepository.ListDetailsAsync(skip, take, cancellationToken);
}

// A closed validator derived from the abstract generic ListQueryValidator<TItem>, for ListQuery<CustomerDetails> only.
public sealed class ListCustomersQueryValidator : ListQueryValidator<CustomerDetails>
{
    protected override int MaxPageSize => 20;
}

// Paged and searchable, so both PagingValidator<TRequest> and SearchPagingValidator<TRequest> validate it,
// as well as its own validator.
public sealed record SearchCustomersQuery(string Search, int Page, int PageSize)
    : IQuery<IReadOnlyList<CustomerDetails>>, IPagedRequest, ISearchableRequest;

// A closed validator derived from the abstract generic ValidatorBase<TRequest>.
public sealed class SearchCustomersQueryValidator : ValidatorBase<SearchCustomersQuery>
{
    public const int MinSearchLength = 2;

    public override void Validate(SearchCustomersQuery request, ValidationState validationState)
        => AddErrorIf(
            validationState,
            request.Search.Trim().Length < MinSearchLength,
            nameof(request.Search),
            $"Search must be at least {MinSearchLength} characters.");
}

public sealed class SearchCustomersQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<SearchCustomersQuery, IReadOnlyList<CustomerDetails>>
{
    public async Task<IResult<IReadOnlyList<CustomerDetails>>> HandleAsync(
        SearchCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomerDetails> customers = await customerRepository.SearchDetailsAsync(
            query.Search.Trim(), (query.Page - 1) * query.PageSize, query.PageSize, cancellationToken);

        return new Success<IReadOnlyList<CustomerDetails>>(customers);
    }
}
