using System.Net;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Infrastructure;
using static DannyGoodacre.Identity.Tests.Infrastructure.HttpAssert;

namespace DannyGoodacre.Identity.Tests;

public sealed class SignInTests
{
    private const string WrongPassword = "Wrong-Password-1";

    [Fact]
    public async Task RegisteringWithTheInviteCode_LetsTheUserSignInStraightAway()
    {
        await using var app = await TestApp.StartAsync();

        var user = await app.RegisterAsync("alice");

        Assert.True(user.IsEnabled);
        Assert.False(user.IsAdmin);
        var me = await (await app.SignInAsync("alice")).GetJson<UserResponse>("users/me");
        Assert.Equal(user.Id, me.Id);
    }

    [Fact]
    public async Task SignIn_WrongPassword_Returns401()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");

        AssertStatus(HttpStatusCode.Unauthorized, await app.CreateClient().Post("sessions", new { username = "alice", password = WrongPassword }));
    }

    [Fact]
    public async Task SignIn_UnknownUser_Returns401()
    {
        await using var app = await TestApp.StartAsync();

        AssertStatus(HttpStatusCode.Unauthorized, await app.CreateClient().Post("sessions", new { username = "nobody", password = TestApp.Password }));
    }

    [Fact]
    public async Task SignIn_AfterTooManyFailures_Returns429WithRetryAfter()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        var client = app.CreateClient();

        for (int attempt = 1; attempt < 5; attempt++)
        {
            AssertStatus(HttpStatusCode.Unauthorized, await client.Post("sessions", new { username = "alice", password = WrongPassword }));
        }

        var lockedOut = await client.Post("sessions", new { username = "alice", password = WrongPassword });
        AssertStatus(HttpStatusCode.TooManyRequests, lockedOut);

        var retryAfter = lockedOut.Headers.RetryAfter?.Delta;
        Assert.NotNull(retryAfter);
        Assert.InRange(retryAfter.Value, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5));

        AssertStatus(HttpStatusCode.TooManyRequests, await client.Post("sessions", new { username = "alice", password = TestApp.Password }));
    }

    [Fact]
    public async Task SignIn_DisabledUser_Returns403()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice", disabled: true);

        var response = await app.CreateClient().Post("sessions", new { username = "alice", password = TestApp.Password });

        AssertStatus(HttpStatusCode.Forbidden, response);
    }

    [Fact]
    public async Task SignOut_EndsSession()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");

        AssertStatus(HttpStatusCode.NoContent, await alice.Delete("sessions"));

        AssertStatus(HttpStatusCode.Unauthorized, await alice.Get("users/me"));
    }

    [Fact]
    public async Task SignIn_MissingFields_ReturnsValidationProblem()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.CreateClient().Post("sessions", new { username = "alice" });

        AssertStatus(HttpStatusCode.BadRequest, response);
        Assert.Contains("Password", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }
}
