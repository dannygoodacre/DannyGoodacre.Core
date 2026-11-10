using DannyGoodacre.Cqrs.Examples.Persistence;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples.Customers;

public sealed record CustomerDetails(Guid PublicId, string Name, string Email, string? PlanCode);

// A read-only query. The pipeline logs and validates it, but never saves data.
// Queries identify a customer by the public ID. The database ID never leaves the persistence layer.
public sealed record GetCustomerQuery(Guid PublicId) : IQuery<CustomerDetails>;

public sealed class GetCustomerQueryValidator : IValidator<GetCustomerQuery>
{
    public void Validate(GetCustomerQuery request, ValidationState validationState)
    {
        if (request.PublicId == Guid.Empty)
        {
            validationState.AddError(nameof(request.PublicId), "Public ID is required.");
        }
    }
}

public sealed class GetCustomerQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerQuery, CustomerDetails>
{
    public async Task<IResult<CustomerDetails>> HandleAsync(
        GetCustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        CustomerDetails? customer = await customerRepository.FindDetailsAsync(query.PublicId, cancellationToken);

        return customer is null
            ? new NotFound<CustomerDetails>()
            : new Success<CustomerDetails>(customer);
    }
}
