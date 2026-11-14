using DannyGoodacre.Cqrs.Examples.Auditing;
using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Listing;
using DannyGoodacre.Cqrs.Examples.Onboarding;
using DannyGoodacre.Cqrs.Examples.Plans;
using DannyGoodacre.Cqrs.Examples.Tests.Infrastructure;
using DannyGoodacre.Cqrs.Examples.Tests.InvalidOpenGenerics;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class OpenGenericRegistrationTests
{
    [Fact]
    public async Task AddCustomerOnboarding_ClosesOpenGenericValidatorsOnlyForRequestsMeetingTheirConstraints()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await using AsyncServiceScope scope = host.CreateScope();

        // Act
        Type[] listValidators = ValidatorTypes<ListQuery<PlanDetails>>(scope.ServiceProvider);
        Type[] searchValidators = ValidatorTypes<SearchCustomersQuery>(scope.ServiceProvider);
        Type[] getPlanValidators = ValidatorTypes<GetPlanQuery>(scope.ServiceProvider);

        // Assert
        Assert.Equal(
            new HashSet<Type> { typeof(PagingValidator<ListQuery<PlanDetails>>), typeof(ListPlansQueryValidator) },
            listValidators.ToHashSet());

        Assert.Equal(
            new HashSet<Type>
            {
                typeof(PagingValidator<SearchCustomersQuery>),
                typeof(SearchPagingValidator<SearchCustomersQuery>),
                typeof(SearchCustomersQueryValidator)
            },
            searchValidators.ToHashSet());

        Assert.Equal([typeof(GetPlanQueryValidator)], getPlanValidators);
    }

    [Fact]
    public async Task AddCustomerOnboarding_RegistersEachClosedValidatorDerivedFromTheAbstractListValidatorForItsOwnList()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await using AsyncServiceScope scope = host.CreateScope();

        // Act
        Type[] customerListValidators = ValidatorTypes<ListQuery<CustomerDetails>>(scope.ServiceProvider);

        // Assert
        Assert.Contains(typeof(ListCustomersQueryValidator), customerListValidators);
        Assert.DoesNotContain(typeof(ListPlansQueryValidator), customerListValidators);
    }

    [Fact]
    public async Task AddCustomerOnboarding_RegistersClosedHooksDerivedFromTheAbstractBaseHook()
    {
        // Arrange
        await using var host = await ExamplesTestHost.CreateAsync();

        await using AsyncServiceScope scope = host.CreateScope();

        // Act
        var registerHooks = scope.ServiceProvider.GetServices<IPostCommitHook<RegisterCustomerCommand, Guid>>();
        var onboardHooks = scope.ServiceProvider.GetServices<IPostCommitHook<OnboardCustomerCommand, Guid>>();

        // Assert
        Assert.IsType<RegisterCustomerAuditHook>(Assert.Single(registerHooks));
        Assert.Contains(onboardHooks, hook => hook is OnboardCustomerAuditHook);
    }

    // The example application does not scan this test assembly, so the invalid types below cannot break it.
    [Theory]
    [InlineData("OpenListQueryHandler<TItem>")]
    [InlineData("DerivedOpenListQueryHandler<TItem>")]
    [InlineData("OpenAuditHook<TCommand>")]
    [InlineData("DerivedOpenAuditHook<TCommand, TResult>")]
    [InlineData("OpenListQueryValidator<TItem>")]
    [InlineData("DerivedListQueryValidator<TItem>")]
    [InlineData("UnusedParameterValidator<TUnused>")]
    [InlineData("TwoParameterValidator<TRequest, TOther>")]
    public void AddCqrsHandlers_WhenScanningAnUnregistrableOpenGenericType_ThrowsNamingIt(string typeName)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCqrsHandlers(typeof(OpenListQueryHandler<>).Assembly));

        Assert.Contains(typeName, exception.Message);
    }

    private static Type[] ValidatorTypes<TRequest>(IServiceProvider services)
        where TRequest : IRequest
        => [.. services.GetServices<IValidator<TRequest>>().Select(v => v.GetType())];
}
