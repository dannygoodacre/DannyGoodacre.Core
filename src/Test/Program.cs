using DannyGoodacre.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace Test;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        builder.Services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<ApplicationContext>(builder.Configuration);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(x =>
        {
            x.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Identity",
                Version = "v0.1"
            });
        });

        var app = builder.Build();

        using IServiceScope scope = app.Services.CreateScope();

        IServiceProvider services = scope.ServiceProvider;

        try
        {
            ApplicationContext context = services.GetRequiredService<ApplicationContext>();

            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while migrating: {ex.Message}");

            throw;
        }

        await services.SynchronizeIdentityPermissionsAsync();

        app.MapIdentityEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();

            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity");
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        await app.RunAsync();
    }
}
