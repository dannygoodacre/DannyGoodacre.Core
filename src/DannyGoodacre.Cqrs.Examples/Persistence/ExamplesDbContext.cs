using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Cqrs.Examples.Persistence;

public sealed class ExamplesDbContext(DbContextOptions<ExamplesDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.HasKey(c => c.Id);

            customer.Property(c => c.Id).ValueGeneratedOnAdd();

            customer.HasIndex(c => c.PublicId).IsUnique();
            customer.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Plan>(plan =>
        {
            plan.HasKey(p => p.Code);
            plan.HasData(
                new Plan { Code = "BASIC", Name = "Basic", MonthlyPrice = 0m },
                new Plan { Code = "PREMIUM", Name = "Premium", MonthlyPrice = 9.99m });
        });

        modelBuilder.Entity<Subscription>(subscription =>
        {
            subscription.HasKey(s => s.Id);

            subscription.Property(s => s.Id).ValueGeneratedOnAdd();

            subscription.HasIndex(s => s.PublicId).IsUnique();
            subscription.HasIndex(s => s.CustomerId).IsUnique();

            subscription.HasOne<Customer>().WithMany().HasForeignKey(s => s.CustomerId);
            subscription.HasOne<Plan>().WithMany().HasForeignKey(s => s.PlanCode);
        });
    }
}
