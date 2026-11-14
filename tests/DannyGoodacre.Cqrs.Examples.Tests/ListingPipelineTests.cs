using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Listing;
using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Cqrs.Examples.Plans;
using DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class ListingPipelineTests
{
    private static Task<IResult<IReadOnlyList<TItem>>> SendAsync<TQuery, TItem>(ExamplesTestHost host, TQuery query)
        where TQuery : IQuery<IReadOnlyList<TItem>>
        => host.InScopeAsync(services => services
            .GetRequiredService<IQueryHandler<TQuery, IReadOnlyList<TItem>>>()
            .HandleAsync(query, TestContext.Current.CancellationToken));

    private static Task<IResult<IReadOnlyList<PlanDetails>>> ListPlansAsync(ExamplesTestHost host, int page, int pageSize)
        => SendAsync<ListQuery<PlanDetails>, PlanDetails>(host, new ListQuery<PlanDetails>(page, pageSize));

    private static Task<IResult<IReadOnlyList<CustomerDetails>>> ListCustomersAsync(ExamplesTestHost host, int page, int pageSize)
        => SendAsync<ListQuery<CustomerDetails>, CustomerDetails>(host, new ListQuery<CustomerDetails>(page, pageSize));

    private static Task<IResult<IReadOnlyList<CustomerDetails>>> SearchCustomersAsync(
        ExamplesTestHost host,
        string search,
        int page,
        int pageSize)
        => SendAsync<SearchCustomersQuery, CustomerDetails>(host, new SearchCustomersQuery(search, page, pageSize));

    private static Task<int> AddCustomersAsync(ExamplesTestHost host, params string[] names)
        => host.InScopeAsync(services =>
        {
            var dbContext = services.GetRequiredService<ExamplesDbContext>();

            foreach (string name in names)
            {
                dbContext.Customers.Add(new Customer
                {
                    PublicId = Guid.NewGuid(),
                    Name = name,
                    Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com"
                });
            }

            return dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

    [Fact]
    public async Task HandleAsync_WhenListingPlans_ReturnsThePageFromTheHandlerDerivedFromTheAbstractBase()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<PlanDetails>> result = await ListPlansAsync(host, page: 2, pageSize: 1);

        // Assert
        IReadOnlyList<PlanDetails> plans = result.ShouldBeSuccess();

        Assert.Equal("PREMIUM", Assert.Single(plans).Code);
    }

    [Fact]
    public async Task HandleAsync_WhenListingCustomers_ReturnsThePageFromTheHandlerDerivedFromTheAbstractBase()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await AddCustomersAsync(host, "Ann Lee", "Jane Doe", "John Doe");

        // Act
        IResult<IReadOnlyList<CustomerDetails>> result = await ListCustomersAsync(host, page: 1, pageSize: 2);

        // Assert
        IReadOnlyList<CustomerDetails> customers = result.ShouldBeSuccess();

        Assert.Equal(["Ann Lee", "Jane Doe"], customers.Select(c => c.Name));
    }

    [Theory]
    [InlineData(0, 10, nameof(IPagedRequest.Page))]
    [InlineData(1, 0, nameof(IPagedRequest.PageSize))]
    [InlineData(1, PagingValidator<ListQuery<PlanDetails>>.MaxPageSize + 1, nameof(IPagedRequest.PageSize))]
    public async Task HandleAsync_WhenListPagingIsInvalid_OpenGenericValidatorShortCircuitsBeforeTheDatabase(
        int page,
        int pageSize,
        string invalidProperty)
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<PlanDetails>> result = await ListPlansAsync(host, page, pageSize);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal([invalidProperty], validationState.Errors.Keys);
        Assert.Empty(host.DatabaseActivity.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenPlanPageIsLargerThanThePlanListAllows_ValidatorDerivedFromTheAbstractListValidatorRejectsIt()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<PlanDetails>> result = await ListPlansAsync(host, page: 1, pageSize: 11);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal(
            ["This list returns at most 10 items per page."],
            validationState.Errors[nameof(IPagedRequest.PageSize)]);
        Assert.Empty(host.DatabaseActivity.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerPageIsLargerThanThePlanListAllows_UsesTheCustomerListLimitInstead()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<CustomerDetails>> result = await ListCustomersAsync(host, page: 1, pageSize: 11);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task HandleAsync_WhenSearchPagingIsInvalid_TheSameOpenGenericValidatorRejectsIt()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<CustomerDetails>> result = await SearchCustomersAsync(host, "Jane", page: 0, pageSize: 10);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal([nameof(IPagedRequest.Page)], validationState.Errors.Keys);
    }

    [Fact]
    public async Task HandleAsync_WhenSearchPageIsPastTheLimit_ValidatorWithSeveralConstraintsRejectsIt()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<CustomerDetails>> result = await SearchCustomersAsync(
            host, "Jane", page: SearchPagingValidator<SearchCustomersQuery>.MaxSearchPage + 1, pageSize: 10);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal([nameof(IPagedRequest.Page)], validationState.Errors.Keys);
        Assert.Empty(host.DatabaseActivity.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenListPageIsPastTheSearchLimit_ValidatorWithSeveralConstraintsDoesNotApply()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<PlanDetails>> result = await ListPlansAsync(
            host, page: SearchPagingValidator<SearchCustomersQuery>.MaxSearchPage + 1, pageSize: 10);

        // Assert
        Assert.Empty(result.ShouldBeSuccess());
    }

    [Fact]
    public async Task HandleAsync_WhenSearchIsTooShort_ValidatorDerivedFromTheAbstractBaseRejectsIt()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        // Act
        IResult<IReadOnlyList<CustomerDetails>> result = await SearchCustomersAsync(host, " J ", page: 1, pageSize: 10);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal([nameof(SearchCustomersQuery.Search)], validationState.Errors.Keys);
    }

    [Fact]
    public async Task HandleAsync_WhenSearchIsValid_ReturnsMatchingCustomers()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await AddCustomersAsync(host, "Ann Lee", "Jane Doe", "Jane Smith");

        // Act
        IResult<IReadOnlyList<CustomerDetails>> result = await SearchCustomersAsync(host, "Jane", page: 1, pageSize: 10);

        // Assert
        IReadOnlyList<CustomerDetails> customers = result.ShouldBeSuccess();

        Assert.Equal(["Jane Doe", "Jane Smith"], customers.Select(c => c.Name));
    }
}
