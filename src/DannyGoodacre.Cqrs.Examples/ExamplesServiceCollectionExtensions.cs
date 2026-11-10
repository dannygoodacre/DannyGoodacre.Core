using DannyGoodacre.Cqrs.Examples.Notifications;
using DannyGoodacre.Cqrs.Examples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DannyGoodacre.Cqrs.Examples;

public static class ExamplesServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCustomerOnboarding(Action<IServiceProvider, DbContextOptionsBuilder> configureDatabase)
        {
            services.AddDbContext<ExamplesDbContext>(configureDatabase);

            services.AddEntityFrameworkUnits<ExamplesDbContext>();

            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IEmailSender, LoggingEmailSender>();
            services.TryAddSingleton(TimeProvider.System);

            services.AddCqrsHandlers([typeof(ExamplesServiceCollectionExtensions).Assembly]);

            return services;
        }
    }
}
