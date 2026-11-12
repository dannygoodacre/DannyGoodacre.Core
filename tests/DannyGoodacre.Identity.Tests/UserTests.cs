using System.Net;
using DannyGoodacre.Identity.Endpoints;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using static DannyGoodacre.Identity.Tests.Infrastructure.HttpAssert;

namespace DannyGoodacre.Identity.Tests;

public sealed class UserTests
{
    [Fact]
    public async Task Registration_WhenUniqueEmailIsRequired_RejectsMissingOrDuplicateEmail()
    {
        await using var app = await TestApp.StartAsync(
            options => options.InitialAdmin!.Email = "admin@example.com",
            services: RequireUniqueEmail);
        var code = await app.GetInviteCodeAsync();
        var client = app.CreateClient();

        var withoutEmail = await client.Post("users", new { username = "alice", password = TestApp.Password, inviteCode = code });
        AssertStatus(HttpStatusCode.BadRequest, withoutEmail);
        Assert.Contains("InvalidEmail", (await withoutEmail.ReadValidationProblemAsync()).Errors.Keys);

        AssertStatus(HttpStatusCode.Created, await client.Post("users", new { username = "alice", password = TestApp.Password, inviteCode = code, email = "alice@example.com" }));
        AssertStatus(HttpStatusCode.Conflict, await client.Post("users", new { username = "bob", password = TestApp.Password, inviteCode = code, email = "alice@example.com" }));
    }

    [Fact]
    public async Task Initialization_WhenUniqueEmailIsRequired_RequiresInitialAdminEmail()
    {
        await using var app = await TestApp.StartAsync(services: RequireUniqueEmail, initialize: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Services.InitializeIdentityAsync(TestContext.Current.CancellationToken));

        Assert.Contains("InvalidEmail", exception.Message);
    }

    [Fact]
    public async Task Registration_ReturnsLocationOfTheNewUser()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.CreateClient().Post("users", new { username = "alice", password = TestApp.Password, inviteCode = await app.GetInviteCodeAsync() });

        AssertStatus(HttpStatusCode.Created, response);
        var user = (await response.ReadAsync<UserResponse>())!;
        Assert.Equal($"/users/{user.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Registration_TakenUsername_Returns409()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");

        AssertStatus(HttpStatusCode.Conflict, await app.CreateClient().Post("users", new { username = "ALICE", password = TestApp.Password, inviteCode = await app.GetInviteCodeAsync() }));
    }

    [Fact]
    public async Task Registration_WeakPassword_ReturnsValidationProblemKeyedByErrorCode()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.CreateClient().Post("users", new { username = "alice", password = "short", inviteCode = await app.GetInviteCodeAsync() });

        AssertStatus(HttpStatusCode.BadRequest, response);
        var errors = (await response.ReadValidationProblemAsync()).Errors;
        Assert.Contains("PasswordTooShort", errors.Keys);
        Assert.Contains("PasswordRequiresDigit", errors.Keys);
    }

    [Fact]
    public async Task CurrentUser_ReturnsAccount()
    {
        await using var app = await TestApp.StartAsync();

        var me = await (await app.SignInAsAdminAsync()).GetJson<UserResponse>("users/me");

        Assert.Equal(await app.GetAdminIdAsync(), me.Id);
        Assert.Equal(TestApp.AdminUsername, me.Username);
        Assert.True(me.IsAdmin);
        Assert.True(me.IsEnabled);
    }

    [Fact]
    public async Task CurrentUser_WhenUserWasDeleted_Returns401AndClearsCookie()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");
        await app.WithScopeAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            TestApp.AssertSucceeded(await userManager.DeleteAsync((await userManager.FindByNameAsync("alice"))!));
        });

        var response = await alice.Get("users/me");

        AssertStatus(HttpStatusCode.Unauthorized, response);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith($".AspNetCore.{IdentityConstants.ApplicationScheme}=;", StringComparison.Ordinal)
                      && cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeletingUserWithRestrictedRelatedData_Returns409_AndDisablingStillWorks()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        await app.WithScopeAsync(async services =>
        {
            var context = services.GetRequiredService<TestIdentityContext>();
            context.GameRecords.Add(new GameRecord { Id = Guid.NewGuid(), UserId = aliceId });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
        var admin = await app.SignInAsAdminAsync();

        AssertStatus(HttpStatusCode.Conflict, await admin.Delete($"users/{aliceId}"));
        Assert.NotNull(await app.FindUserAsync("alice"));

        AssertStatus(HttpStatusCode.NoContent, await admin.Delete($"users/{aliceId}/enabled"));
        Assert.False((await admin.GetJson<UserResponse>($"users/{aliceId}")).IsEnabled);
    }

    [Fact]
    public async Task DeletingSelf_DeletesTheUserAndSignsOut()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");

        AssertStatus(HttpStatusCode.NoContent, await alice.Delete($"users/{aliceId}"));

        Assert.Null(await app.FindUserAsync("alice"));
        AssertStatus(HttpStatusCode.Unauthorized, await alice.Get("users/me"));
    }

    [Fact]
    public async Task Admin_CanDeleteAnotherUser()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");

        AssertStatus(HttpStatusCode.NoContent, await (await app.SignInAsAdminAsync()).Delete($"users/{aliceId}"));

        Assert.Null(await app.FindUserAsync("alice"));
    }

    [Fact]
    public async Task ConcurrentUpdatesToSameUser_ProduceOneSuccessAndOneConflict()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        await using var firstScope = app.Services.CreateAsyncScope();
        await using var secondScope = app.Services.CreateAsyncScope();
        var firstManager = firstScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var secondManager = secondScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var first = (await firstManager.FindByNameAsync("alice"))!;
        var second = (await secondManager.FindByNameAsync("alice"))!;

        first.Email = "first@example.com";
        var firstResult = await firstManager.UpdateAsync(first);
        second.Email = "second@example.com";
        var secondResult = await secondManager.UpdateAsync(second);

        Assert.True(firstResult.Succeeded);
        Assert.Equal([nameof(IdentityErrorDescriber.ConcurrencyFailure)], secondResult.Errors.Select(error => error.Code));
        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsAssignableFrom<IStatusCodeHttpResult>(Problems.From(secondResult)).StatusCode);
    }

    private static void RequireUniqueEmail(IServiceCollection services)
        => services.Configure<IdentityOptions>(options => options.User.RequireUniqueEmail = true);
}
