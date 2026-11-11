using System.Security.Claims;
using DannyGoodacre.Identity;
using DannyGoodacre.Identity.Example;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExampleContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Example")));

builder.Services.AddUsernameIdentity<ExampleContext>(builder.Configuration);

// Keeps the keys that encrypt the sign-in cookie in the database, so sessions survive restarts and redeploys. A fixed
// application name keeps them valid if the app moves to another folder or host.
builder.Services.AddDataProtection()
    .SetApplicationName("DannyGoodacre.Identity.Example")
    .PersistKeysToDbContext<ExampleContext>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("identity").MapIdentityEndpoints();

// Any signed-in user: their own player profile and games.
app.MapGet("games/me", async (ClaimsPrincipal user, ExampleContext db) =>
    {
        Guid? userId = user.GetUserId();

        var player = await db.Players
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.DisplayName,
                p.Chips,
                Games = p.Games.OrderBy(g => g.Id).Select(g => new { g.Id, g.Stake, g.Payout, g.PlayedAt }).ToList()
            })
            .SingleOrDefaultAsync();

        return player is null ? Results.NotFound() : Results.Ok(player);
    })
    .RequireAuthorization();

// Admins only.
app.MapGet("games", async (ExampleContext db) =>
    {
        var games = await db.Games
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .Join(db.Users, g => g.PlayerId, u => u.Id, (g, u) => new { g.Id, Username = u.UserName, g.Stake, g.Payout, g.PlayedAt })
            .ToListAsync();

        return Results.Ok(games);
    })
    .RequireAuthorization(IdentityDefaults.AdminPolicy);

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ExampleContext>().Database.MigrateAsync();
}

await app.Services.InitializeIdentityAsync();

if (app.Environment.IsDevelopment())
{
    await DemoData.SeedAsync(app.Services);
}

await app.RunAsync();
