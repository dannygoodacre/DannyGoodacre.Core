using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Listing;

public interface IPagedRequest
{
    int Page { get; }

    int PageSize { get; }
}

public interface ISearchableRequest
{
    string Search { get; }
}

// A generic request: ListQuery<CustomerDetails> and ListQuery<PlanDetails> are separate requests with separate handlers.
public sealed record ListQuery<TItem>(int Page, int PageSize) : IQuery<IReadOnlyList<TItem>>, IPagedRequest;

// An abstract generic base handler. AddCqrsHandlers does not scan abstract types, so it registers only the closed
// handlers derived from this one, such as ListPlansQueryHandler. A non-abstract open generic handler would make
// AddCqrsHandlers throw at startup, because the pipeline cannot be built for a request type that is not known.
public abstract class ListQueryHandler<TItem> : IQueryHandler<ListQuery<TItem>, IReadOnlyList<TItem>>
{
    public async Task<IResult<IReadOnlyList<TItem>>> HandleAsync(
        ListQuery<TItem> query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TItem> items = await ReadPageAsync(
            (query.Page - 1) * query.PageSize, query.PageSize, cancellationToken);

        return new Success<IReadOnlyList<TItem>>(items);
    }

    protected abstract Task<IReadOnlyList<TItem>> ReadPageAsync(int skip, int take, CancellationToken cancellationToken);
}
