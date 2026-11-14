using DannyGoodacre.Cqrs.Examples.Customers;
using DannyGoodacre.Cqrs.Examples.Plans;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Cqrs.Examples.Persistence;

// The repositories add and read entities on the scoped DbContext.
// They do not call SaveChangesAsync. The CQRS pipeline does that.

public interface ICustomerRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    void Add(Customer customer);

    Task<int> GetIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<CustomerDetails?> FindDetailsAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDetails>> ListDetailsAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDetails>> SearchDetailsAsync(
        string search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public interface IPlanRepository
{
    Task<PlanDetails?> FindAsync(string planCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanDetails>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}

public interface ISubscriptionRepository
{
    void Add(Subscription subscription);
}

public sealed class CustomerRepository(ExamplesDbContext dbContext) : ICustomerRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => dbContext.Customers.AnyAsync(c => c.Email == email, cancellationToken);

    public void Add(Customer customer)
        => dbContext.Customers.Add(customer);

    // The customer must exist. A missing row is a fault, so SingleAsync throws.
    public Task<int> GetIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => dbContext.Customers
            .Where(c => c.PublicId == publicId)
            .Select(c => c.Id)
            .SingleAsync(cancellationToken);

    public Task<CustomerDetails?> FindDetailsAsync(Guid publicId, CancellationToken cancellationToken = default)
        => SelectDetails(dbContext.Customers.AsNoTracking().Where(c => c.PublicId == publicId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerDetails>> ListDetailsAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => await SelectDetails(dbContext.Customers.AsNoTracking().OrderBy(c => c.Id).Skip(skip).Take(take))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerDetails>> SearchDetailsAsync(
        string search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => await SelectDetails(dbContext.Customers
                .AsNoTracking()
                .Where(c => c.Name.Contains(search))
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(take))
            .ToListAsync(cancellationToken);

    private IQueryable<CustomerDetails> SelectDetails(IQueryable<Customer> customers)
        => customers.Select(c => new CustomerDetails(
            c.PublicId,
            c.Name,
            c.Email,
            dbContext.Subscriptions
                .Where(s => s.CustomerId == c.Id)
                .Select(s => s.PlanCode)
                .FirstOrDefault()));
}

public sealed class PlanRepository(ExamplesDbContext dbContext) : IPlanRepository
{
    public Task<PlanDetails?> FindAsync(string planCode, CancellationToken cancellationToken = default)
        => dbContext.Plans
            .AsNoTracking()
            .Where(p => p.Code == planCode)
            .Select(p => new PlanDetails(p.Code, p.Name, p.MonthlyPrice))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanDetails>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
        => await dbContext.Plans
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Skip(skip)
            .Take(take)
            .Select(p => new PlanDetails(p.Code, p.Name, p.MonthlyPrice))
            .ToListAsync(cancellationToken);
}

public sealed class SubscriptionRepository(ExamplesDbContext dbContext) : ISubscriptionRepository
{
    public void Add(Subscription subscription)
        => dbContext.Subscriptions.Add(subscription);
}
