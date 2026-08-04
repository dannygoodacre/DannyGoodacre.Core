using System.Reflection;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Application;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            return services
                .AddScoped<IPasswordHashingService, PasswordHashingService>()
                .AddScoped<IPasswordValidatorService, PasswordValidatorService>()
                .AddCommandHandlers(Assembly.GetExecutingAssembly())
                .AddQueryHandlers(Assembly.GetExecutingAssembly());
        }
    }
}
