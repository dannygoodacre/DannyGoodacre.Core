using System.Net;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Infrastructure;
using static DannyGoodacre.Identity.Tests.Infrastructure.HttpAssert;

namespace DannyGoodacre.Identity.Tests;

public sealed class AdminTests
{
    [Fact]
    public async Task AdminPolicy_AdmitsAdminsOnly()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");

        AssertStatus(HttpStatusCode.NoContent, await (await app.SignInAsAdminAsync()).Get("admin-only"));
        AssertStatus(HttpStatusCode.Forbidden, await (await app.SignInAsync("alice")).Get("admin-only"));
        AssertStatus(HttpStatusCode.Unauthorized, await app.CreateClient().Get("admin-only"));
    }

    [Fact]
    public async Task AdminEndpoints_RefuseRegularUsers()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        var bobId = await app.CreateUserAsync("bob");
        var alice = await app.SignInAsync("alice");

        AssertStatus(HttpStatusCode.Forbidden, await alice.Get("users"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Get($"users/{bobId}"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Delete($"users/{bobId}/enabled"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Put($"users/{aliceId}/admin"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Post($"users/{bobId}/password-reset"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Delete($"users/{bobId}"));
    }

    [Fact]
    public async Task DisablingAUser_EndsTheirSessions_AndEnablingLetsThemBackIn()
    {
        await using var app = await TestApp.StartAsync(services: TestApp.ValidateEveryRequest);
        var aliceId = await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");
        var admin = await app.SignInAsAdminAsync();

        AssertStatus(HttpStatusCode.NoContent, await admin.Delete($"users/{aliceId}/enabled"));

        AssertStatus(HttpStatusCode.Unauthorized, await alice.Get("users/me"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Post("sessions", new { username = "alice", password = TestApp.Password }));
        Assert.False((await admin.GetJson<UserResponse>($"users/{aliceId}")).IsEnabled);

        AssertStatus(HttpStatusCode.NoContent, await admin.Put($"users/{aliceId}/enabled"));
        AssertStatus(HttpStatusCode.NoContent, await alice.Post("sessions", new { username = "alice", password = TestApp.Password }));
    }

    [Fact]
    public async Task EnableAndDisable_AreIdempotent()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        var admin = await app.SignInAsAdminAsync();

        AssertStatus(HttpStatusCode.NoContent, await admin.Put($"users/{aliceId}/enabled"));
        AssertStatus(HttpStatusCode.NoContent, await admin.Delete($"users/{aliceId}/enabled"));
        AssertStatus(HttpStatusCode.NoContent, await admin.Delete($"users/{aliceId}/enabled"));
        AssertStatus(HttpStatusCode.NoContent, await admin.Put($"users/{aliceId}/enabled"));
        AssertStatus(HttpStatusCode.NoContent, await admin.Put($"users/{aliceId}/enabled"));
    }

    [Fact]
    public async Task MakingAUserAdmin_ReachesTheirSessionAfterValidation_WithoutSigningThemOut()
    {
        await using var app = await TestApp.StartAsync(services: TestApp.ValidateEveryRequest);
        var aliceId = await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");
        var admin = await app.SignInAsAdminAsync();
        AssertStatus(HttpStatusCode.Forbidden, await alice.Get("admin-only"));

        AssertStatus(HttpStatusCode.NoContent, await admin.Put($"users/{aliceId}/admin"));
        AssertStatus(HttpStatusCode.NoContent, await alice.Get("admin-only"));
        Assert.True((await alice.GetJson<UserResponse>("users/me")).IsAdmin);

        AssertStatus(HttpStatusCode.NoContent, await admin.Delete($"users/{aliceId}/admin"));
        AssertStatus(HttpStatusCode.Forbidden, await alice.Get("admin-only"));
        AssertStatus(HttpStatusCode.OK, await alice.Get("users/me"));
    }

    [Fact]
    public async Task AdminChanges_KeepTheOldCookieUntilValidation()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice", admin: true);
        var alice = await app.SignInAsync("alice");

        AssertStatus(HttpStatusCode.NoContent, await (await app.SignInAsAdminAsync()).Delete($"users/{aliceId}/admin"));

        AssertStatus(HttpStatusCode.NoContent, await alice.Get("admin-only"));
    }

    [Fact]
    public async Task LastAdmin_CannotBeDeletedDisabledOrRemoved()
    {
        await using var app = await TestApp.StartAsync();
        var adminId = await app.GetAdminIdAsync();
        await app.CreateUserAsync("disabled-admin", admin: true, disabled: true);
        var admin = await app.SignInAsAdminAsync();

        AssertStatus(HttpStatusCode.Conflict, await admin.Delete($"users/{adminId}"));
        AssertStatus(HttpStatusCode.Conflict, await admin.Delete($"users/{adminId}/admin"));
        AssertStatus(HttpStatusCode.Conflict, await admin.Delete($"users/{adminId}/enabled"));

        var secondId = await app.CreateUserAsync("second", admin: true);
        var second = await app.SignInAsync("second");
        AssertStatus(HttpStatusCode.NoContent, await second.Delete($"users/{adminId}/admin"));
        AssertStatus(HttpStatusCode.Conflict, await second.Delete($"users/{secondId}/admin"));
    }

    [Fact]
    public async Task Admins_CannotDisableThemselves()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("second", admin: true);
        var adminId = await app.GetAdminIdAsync();

        AssertStatus(HttpStatusCode.Conflict, await (await app.SignInAsAdminAsync()).Delete($"users/{adminId}/enabled"));
    }

    [Fact]
    public async Task ListUsers_ShowsAdminAndEnabledState()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice", disabled: true);
        await app.CreateUserAsync("bob", admin: true);

        var users = await (await app.SignInAsAdminAsync()).GetJson<List<UserResponse>>("users");

        Assert.Equal(
            [(TestApp.AdminUsername, true, true), ("alice", false, false), ("bob", true, true)],
            users.Select(user => (user.Username, user.IsAdmin, user.IsEnabled)));
    }

    [Fact]
    public async Task GetUser_UnknownId_Returns404()
    {
        await using var app = await TestApp.StartAsync();

        AssertStatus(HttpStatusCode.NotFound, await (await app.SignInAsAdminAsync()).Get($"users/{Guid.NewGuid()}"));
    }
}
