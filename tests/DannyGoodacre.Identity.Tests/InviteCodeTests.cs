using System.Net;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Infrastructure;
using static DannyGoodacre.Identity.Tests.Infrastructure.HttpAssert;

namespace DannyGoodacre.Identity.Tests;

public sealed class InviteCodeTests
{
    [Fact]
    public async Task Registration_WithoutAValidInviteCode_IsRefused()
    {
        await using var app = await TestApp.StartAsync();
        var client = app.CreateClient();

        var missing = await client.Post("users", new { username = "alice", password = TestApp.Password });
        AssertStatus(HttpStatusCode.BadRequest, missing);
        Assert.Contains("InviteCode", (await missing.ReadValidationProblemAsync()).Errors.Keys);

        AssertStatus(HttpStatusCode.Forbidden, await client.Post("users", new { username = "alice", password = TestApp.Password, inviteCode = Guid.NewGuid() }));
        AssertStatus(HttpStatusCode.Forbidden, await client.Post("users", new { username = "alice", password = TestApp.Password, inviteCode = "not-a-guid" }));

        Assert.Null(await app.FindUserAsync("alice"));
    }

    [Fact]
    public async Task Admin_CanReadTheInviteCode()
    {
        await using var app = await TestApp.StartAsync();

        var code = await (await app.SignInAsAdminAsync()).GetJson<InviteCodeResponse>("invite-code");

        Assert.Equal(await app.GetInviteCodeAsync(), code.Code);
    }

    [Fact]
    public async Task RotatingTheInviteCode_RefusesTheOldCodeAndAcceptsTheNewOne()
    {
        await using var app = await TestApp.StartAsync();
        var oldCode = await app.GetInviteCodeAsync();
        var admin = await app.SignInAsAdminAsync();

        var rotated = await admin.Post("invite-code");
        AssertStatus(HttpStatusCode.OK, rotated);
        var newCode = (await rotated.ReadAsync<InviteCodeResponse>())!.Code;

        Assert.NotEqual(oldCode, newCode);
        var client = app.CreateClient();
        AssertStatus(HttpStatusCode.Forbidden, await client.Post("users", new { username = "alice", password = TestApp.Password, inviteCode = oldCode }));
        AssertStatus(HttpStatusCode.Created, await client.Post("users", new { username = "alice", password = TestApp.Password, inviteCode = newCode }));
    }

    [Fact]
    public async Task RotatingTheInviteCode_DoesNotAffectExistingAccounts()
    {
        await using var app = await TestApp.StartAsync();
        await app.RegisterAsync("alice");

        AssertStatus(HttpStatusCode.OK, await (await app.SignInAsAdminAsync()).Post("invite-code"));

        await app.SignInAsync("alice");
    }

    [Fact]
    public async Task InviteCodeEndpoints_RequireAdmin()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");

        AssertStatus(HttpStatusCode.Forbidden, await alice.Get("invite-code"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Post("invite-code"));
        AssertStatus(HttpStatusCode.Unauthorized, await app.CreateClient().Get("invite-code"));
    }
}
