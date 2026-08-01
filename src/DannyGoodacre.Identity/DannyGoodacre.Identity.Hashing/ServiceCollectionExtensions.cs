using DannyGoodacre.Identity.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Hashing;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHashingService()
            => services.AddScoped<IPasswordHashingService, PasswordHashingService>();
    }
}
