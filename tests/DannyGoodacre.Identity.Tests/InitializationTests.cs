using DannyGoodacre.Identity.Data;
using DannyGoodacre.Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Tests;

public sealed class InitializationTests
{
    [Fact]
    public async Task Initialization_CreatesAdminRoleInviteCodeAndInitialAdmin()
    {
        await using var app = await TestApp.StartAsync();

        await app.WithScopeAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            var admin = (await userManager.FindByNameAsync(TestApp.AdminUsername))!;
            Assert.True(await userManager.IsInRoleAsync(admin, IdentityDefaults.AdminRole));
        });
        Assert.NotEqual(Guid.Empty, await app.GetInviteCodeAsync());
    }

    [Fact]
    public async Task Initialization_IsIdempotent_AndKeepsTheInviteCode()
    {
        await using var app = await TestApp.StartAsync();
        var before = await SnapshotAsync(app);

        await app.Services.InitializeIdentityAsync(TestContext.Current.CancellationToken);
        await app.Services.InitializeIdentityAsync(TestContext.Current.CancellationToken);

        Assert.Equal(before, await SnapshotAsync(app));
    }

    [Fact]
    public async Task Initialization_CreatesInitialAdminOnlyOnce()
    {
        await using var app = await TestApp.StartAsync();
        await app.WithScopeAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            var admin = (await userManager.FindByNameAsync(TestApp.AdminUsername))!;
            TestApp.AssertSucceeded(await userManager.ChangePasswordAsync(admin, TestApp.AdminPassword, "Changed-Password-1"));
        });

        await app.Services.InitializeIdentityAsync(TestContext.Current.CancellationToken);

        await app.SignInAsync(TestApp.AdminUsername, "Changed-Password-1");
        Assert.Equal(1, await CountAsync(app, context => context.Users));
    }

    [Fact]
    public async Task Initialization_WithoutInitialAdmin_ThrowsOnlyWhenThereIsNoEnabledAdmin()
    {
        using var database = new TempDatabase();

        await using (var empty = await TestApp.StartAsync(options => options.InitialAdmin = null, databasePath: database.Path, initialize: false))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => empty.Services.InitializeIdentityAsync(TestContext.Current.CancellationToken));
        }

        await using (await TestApp.StartAsync(databasePath: database.Path))
        {
        }

        await using var seeded = await TestApp.StartAsync(options => options.InitialAdmin = null, databasePath: database.Path);
        await seeded.SignInAsAdminAsync();
    }

    [Fact]
    public async Task Initialization_FailingPartway_LeavesDatabaseUnchanged()
    {
        await using var app = await TestApp.StartAsync(options => options.InitialAdmin!.Password = "short", initialize: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Services.InitializeIdentityAsync(TestContext.Current.CancellationToken));

        Assert.Contains("PasswordTooShort", exception.Message);
        Assert.Equal(0, await CountAsync(app, context => context.Roles));
        Assert.Equal(0, await CountAsync(app, context => context.Set<InviteCode>()));
        Assert.Equal(0, await CountAsync(app, context => context.Users));
    }

    private static Task<int> CountAsync<T>(TestApp app, Func<TestIdentityContext, IQueryable<T>> set)
        => app.WithScopeAsync(services =>
            set(services.GetRequiredService<TestIdentityContext>()).CountAsync(TestContext.Current.CancellationToken));

    private static Task<string> SnapshotAsync(TestApp app)
        => app.WithScopeAsync(async services =>
        {
            var context = services.GetRequiredService<TestIdentityContext>();
            var cancellationToken = TestContext.Current.CancellationToken;

            var roles = await context.Roles.OrderBy(role => role.Name).Select(role => role.Id + role.Name!).ToListAsync(cancellationToken);
            var users = await context.Users.OrderBy(user => user.UserName).Select(user => user.Id + user.UserName!).ToListAsync(cancellationToken);
            var memberships = await context.UserRoles.OrderBy(role => role.UserId).Select(role => role.UserId + "/" + role.RoleId).ToListAsync(cancellationToken);
            var codes = await context.Set<InviteCode>().Select(code => code.Code.ToString()).ToListAsync(cancellationToken);

            return string.Join("|", roles.Concat(users).Concat(memberships).Concat(codes));
        });
}
