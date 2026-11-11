using System.Net;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static DannyGoodacre.Identity.Tests.Infrastructure.HttpAssert;

namespace DannyGoodacre.Identity.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task CookiesAndResetTokens_SurviveRestart()
    {
        using var database = new TempDatabase();
        var cookies = new CookieContainer();
        PasswordResetTokenResponse token;

        await using (var first = await TestApp.StartAsync(databasePath: database.Path))
        {
            var aliceId = await first.CreateUserAsync("alice");
            var admin = await first.SignInAsAdminAsync(cookies);
            token = (await (await admin.Post($"users/{aliceId}/password-reset")).ReadAsync<PasswordResetTokenResponse>())!;
        }

        await using var second = await TestApp.StartAsync(databasePath: database.Path);

        var me = await second.CreateClient(cookies).GetJson<UserResponse>("users/me");
        Assert.Equal(TestApp.AdminUsername, me.Username);

        AssertStatus(HttpStatusCode.NoContent, await second.CreateClient().Post("password-reset", new { username = "alice", token = token.Token, newPassword = "New-Password-2" }));

        Assert.NotEqual(0, await CountKeysAsync(second));
    }

    [Fact]
    public async Task EachEndpointGroup_CanBeMappedOnItsOwn()
    {
        await using var sessionsOnly = await TestApp.StartAsync(map: app => app.MapGroup("auth").MapIdentitySessionEndpoints());
        await sessionsOnly.CreateUserAsync("alice");
        var client = sessionsOnly.CreateClient();
        AssertStatus(HttpStatusCode.NoContent, await client.Post("auth/sessions", new { username = "alice", password = TestApp.Password }));
        AssertStatus(HttpStatusCode.NotFound, await client.Get("auth/users/me"));
        AssertStatus(HttpStatusCode.NotFound, await client.Get("users/me"));

        await using var usersOnly = await TestApp.StartAsync(map: app => app.MapIdentityUserEndpoints());
        AssertStatus(HttpStatusCode.Created, await usersOnly.CreateClient().Post("users", new { username = "alice", password = TestApp.Password, inviteCode = await usersOnly.GetInviteCodeAsync() }));
        AssertStatus(HttpStatusCode.NotFound, await usersOnly.CreateClient().Post("sessions", new { username = "alice", password = TestApp.Password }));
    }

    [Fact]
    public async Task LocationHeaders_IncludeThePrefix()
    {
        await using var app = await TestApp.StartAsync(map: app => app.MapGroup("identity").MapIdentityEndpoints());
        var admin = app.CreateClient();
        AssertStatus(HttpStatusCode.NoContent, await admin.Post("identity/sessions", new { username = TestApp.AdminUsername, password = TestApp.AdminPassword }));

        var user = await admin.Post("identity/users", new { username = "alice", password = TestApp.Password, inviteCode = await app.GetInviteCodeAsync() });

        AssertStatus(HttpStatusCode.Created, user);
        Assert.StartsWith("/identity/users/", user.Headers.Location!.OriginalString);
        AssertStatus(HttpStatusCode.OK, await admin.Get(user.Headers.Location.OriginalString.TrimStart('/')));
    }

    [Fact]
    public void AddAuthenticationCalledAgain_KeepsIdentityDefaultScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestIdentityContext>(options => options.UseSqlite("DataSource=:memory:"));

        services.AddUsernameIdentity<TestIdentityContext>();
        services.AddAuthentication();
        services.AddAuthentication(options => options.RequireAuthenticatedSignIn = false);

        using var provider = services.BuildServiceProvider();
        Assert.Equal(
            IdentityConstants.ApplicationScheme,
            provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value.DefaultScheme);
    }

    [Fact]
    public void IdentityOptionsConfiguredAfterRegistration_OverridePackageDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestIdentityContext>(options => options.UseSqlite("DataSource=:memory:"));

        services.AddUsernameIdentity<TestIdentityContext>();
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(10));
        services.Configure<IdentityOptions>(options => options.Password.RequiredLength = 12);

        using var provider = services.BuildServiceProvider();
        Assert.Equal(TimeSpan.FromMinutes(10), provider.GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value.ValidationInterval);
        Assert.Equal(12, provider.GetRequiredService<IOptions<IdentityOptions>>().Value.Password.RequiredLength);
    }

    [Fact]
    public async Task Defaults_MatchTheReadme()
    {
        await using var app = await TestApp.StartAsync();
        var identity = app.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;
        var cookie = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);

        Assert.False(identity.User.RequireUniqueEmail);
        Assert.Equal(5, identity.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(5), identity.Lockout.DefaultLockoutTimeSpan);
        Assert.Equal(TimeSpan.FromMinutes(1), app.Services.GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value.ValidationInterval);
        Assert.Equal(TimeSpan.FromDays(1), app.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan);
        Assert.Equal(TimeSpan.FromDays(14), cookie.ExpireTimeSpan);
        Assert.True(cookie.SlidingExpiration);
        Assert.Equal(SameSiteMode.Lax, cookie.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, cookie.Cookie.SecurePolicy);
        Assert.True(cookie.Cookie.HttpOnly);
    }

    private static Task<int> CountKeysAsync(TestApp app)
        => app.WithScopeAsync(services => services.GetRequiredService<TestIdentityContext>()
            .DataProtectionKeys.CountAsync(TestContext.Current.CancellationToken));
}
