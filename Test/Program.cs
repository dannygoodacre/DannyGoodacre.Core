using DannyGoodacre.Core.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace Test;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        builder.Services.AddDbContext<ApplicationContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<ApplicationContext>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
        });

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

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            context.Database.Migrate();
        }

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


        app.Run();
    }
}
