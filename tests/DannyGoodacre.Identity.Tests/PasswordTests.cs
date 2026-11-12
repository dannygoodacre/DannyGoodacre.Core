using System.Net;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using static DannyGoodacre.Identity.Tests.Infrastructure.HttpAssert;

namespace DannyGoodacre.Identity.Tests;

public sealed class PasswordTests
{
    private const string NewPassword = "New-Password-2";

    [Fact]
    public async Task ChangingPassword_KeepsCallerSignedIn_AndEndsOtherSessions()
    {
        await using var app = await TestApp.StartAsync(services: TestApp.ValidateEveryRequest);
        await app.CreateUserAsync("alice");
        var first = await app.SignInAsync("alice");
        var second = await app.SignInAsync("alice");

        AssertStatus(HttpStatusCode.NoContent, await first.Put("users/me/password", new { currentPassword = TestApp.Password, newPassword = NewPassword }));

        AssertStatus(HttpStatusCode.OK, await first.Get("users/me"));
        AssertStatus(HttpStatusCode.Unauthorized, await second.Get("users/me"));
        await app.SignInAsync("alice", NewPassword);
    }

    [Fact]
    public async Task ChangingPassword_WithWrongCurrentPassword_ReturnsValidationProblem()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        var alice = await app.SignInAsync("alice");

        var response = await alice.Put("users/me/password", new { currentPassword = "Wrong-Password-1", newPassword = NewPassword });

        AssertStatus(HttpStatusCode.BadRequest, response);
        Assert.Contains("PasswordMismatch", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task PasswordReset_LetsUserSignInWithNewPassword_EndsSessions_AndTokenWorksOnce()
    {
        await using var app = await TestApp.StartAsync(services: TestApp.ValidateEveryRequest);
        var aliceId = await app.CreateUserAsync("alice");
        var aliceSession = await app.SignInAsync("alice");
        var admin = await app.SignInAsAdminAsync();

        var issued = await admin.Post($"users/{aliceId}/password-reset");
        AssertStatus(HttpStatusCode.OK, issued);
        var token = (await issued.ReadAsync<PasswordResetTokenResponse>())!;
        Assert.Equal("alice", token.Username);

        var anonymous = app.CreateClient();
        AssertStatus(HttpStatusCode.NoContent, await anonymous.Post("password-reset", new { username = "alice", token = token.Token, newPassword = NewPassword }));

        await app.SignInAsync("alice", NewPassword);
        AssertStatus(HttpStatusCode.Unauthorized, await aliceSession.Get("users/me"));
        AssertStatus(HttpStatusCode.BadRequest, await anonymous.Post("password-reset", new { username = "alice", token = token.Token, newPassword = "Another-Password-3" }));
    }

    [Fact]
    public async Task PasswordReset_ClearsLockout()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        var client = app.CreateClient();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await client.Post("sessions", new { username = "alice", password = "Wrong-Password-1" });
        }

        AssertStatus(HttpStatusCode.TooManyRequests, await client.Post("sessions", new { username = "alice", password = TestApp.Password }));

        var token = await IssueTokenAsync(app, aliceId);
        AssertStatus(HttpStatusCode.NoContent, await client.Post("password-reset", new { username = "alice", token, newPassword = NewPassword }));

        AssertStatus(HttpStatusCode.NoContent, await client.Post("sessions", new { username = "alice", password = NewPassword }));
    }

    [Fact]
    public async Task PasswordResetToken_IsNotIssuedForDisabledUser()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice", disabled: true);

        AssertStatus(HttpStatusCode.Conflict, await (await app.SignInAsAdminAsync()).Post($"users/{aliceId}/password-reset"));
    }

    [Fact]
    public async Task PasswordReset_WithTokenIssuedBeforeDisabling_FailsAndLeavesUserDisabled()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        var token = await IssueTokenAsync(app, aliceId);
        AssertStatus(HttpStatusCode.NoContent, await (await app.SignInAsAdminAsync()).Delete($"users/{aliceId}/enabled"));

        var client = app.CreateClient();
        AssertStatus(HttpStatusCode.BadRequest, await client.Post("password-reset", new { username = "alice", token, newPassword = NewPassword }));

        AssertStatus(HttpStatusCode.Forbidden, await client.Post("sessions", new { username = "alice", password = TestApp.Password }));
    }

    [Fact]
    public async Task PasswordResetToken_IsNotIssuedForCaller()
    {
        await using var app = await TestApp.StartAsync();
        var adminId = await app.GetAdminIdAsync();

        AssertStatus(HttpStatusCode.Conflict, await (await app.SignInAsAdminAsync()).Post($"users/{adminId}/password-reset"));
    }

    [Fact]
    public async Task PasswordReset_ReturnsSameResponseForUnknownUsernameAndBadToken()
    {
        await using var app = await TestApp.StartAsync();
        await app.CreateUserAsync("alice");
        var client = app.CreateClient();

        var unknownUser = await client.Post("password-reset", new { username = "nobody", token = "token", newPassword = NewPassword });
        var badToken = await client.Post("password-reset", new { username = "alice", token = "token", newPassword = NewPassword });

        AssertStatus(HttpStatusCode.BadRequest, unknownUser);
        AssertStatus(HttpStatusCode.BadRequest, badToken);
        var unknownProblem = await unknownUser.ReadProblemAsync();
        var badTokenProblem = await badToken.ReadProblemAsync();
        Assert.Equal(unknownProblem.Title, badTokenProblem.Title);
        Assert.Equal(unknownProblem.Detail, badTokenProblem.Detail);
    }

    [Fact]
    public async Task PasswordReset_WeakPassword_ReturnsValidationProblem()
    {
        await using var app = await TestApp.StartAsync();
        var aliceId = await app.CreateUserAsync("alice");
        var token = await IssueTokenAsync(app, aliceId);

        var response = await app.CreateClient().Post("password-reset", new { username = "alice", token, newPassword = "short" });

        AssertStatus(HttpStatusCode.BadRequest, response);
        Assert.Contains("PasswordTooShort", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task PasswordResetToken_ExpiresAfterConfiguredLifespan()
    {
        await using var app = await TestApp.StartAsync(services: services =>
            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.Zero));
        var aliceId = await app.CreateUserAsync("alice");
        var token = await IssueTokenAsync(app, aliceId);

        var response = await app.CreateClient().Post("password-reset", new { username = "alice", token, newPassword = NewPassword });

        AssertStatus(HttpStatusCode.BadRequest, response);
    }

    private static async Task<string> IssueTokenAsync(TestApp app, Guid userId)
    {
        var response = await (await app.SignInAsAdminAsync()).Post($"users/{userId}/password-reset");

        AssertStatus(HttpStatusCode.OK, response);

        return (await response.ReadAsync<PasswordResetTokenResponse>())!.Token;
    }
}
