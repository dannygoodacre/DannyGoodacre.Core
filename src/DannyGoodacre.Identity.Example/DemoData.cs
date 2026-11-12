using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DannyGoodacre.Identity.Example;

/// <summary>
/// Gives every user without one a player profile and a few games, so the endpoints have data to return.
/// </summary>
public static class DemoData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ExampleContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var users = await userManager.Users
            .Where(user => !context.Players.Any(player => player.UserId == user.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            context.Players.Add(new Player
            {
                UserId = user.Id,
                DisplayName = user.UserName!,
                Chips = 1000,
                Games =
                [
                    new Game { Stake = 50, Payout = 100, PlayedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                    new Game { Stake = 25, Payout = 0, PlayedAt = DateTimeOffset.UtcNow.AddHours(-1) }
                ]
            });
        }

        await context.SaveChangesAsync();
    }
}
