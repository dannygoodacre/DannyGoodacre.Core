using System.Reflection;
using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Application;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
            => services
                .AddScoped<IPasswordValidatorService, PasswordValidatorService>()
                .AddCommandHandlers(Assembly.GetExecutingAssembly())
                .AddQueryHandlers(Assembly.GetExecutingAssembly());
    }
}
