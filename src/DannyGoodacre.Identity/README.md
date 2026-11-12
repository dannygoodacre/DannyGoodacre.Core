# DannyGoodacre.Identity

Accounts and sign-in for a small, private ASP.NET Core app, built on ASP.NET Core Identity:

- Accounts have a username; email is optional.
- Registering requires the current invite code, a GUID that admins can view and replace.
- Admins can disable and re-enable individual accounts, issue password reset tokens, and make other users admins.
- Sign-in uses Identity's cookie.
- There are two kinds of user: admins, who can do everything, and everyone else.

The package is a thin layer over stock Identity. It registers Identity with a few required settings and adds endpoints, the invite code, and admin management. Everything else, such as password rules, lockout, and the cookie, is configured through Identity's own options. It does not use `MapIdentityApi`, which requires email, issues bearer tokens as well as cookies, and has no invite code or admin endpoints.

## Installation

```shell
dotnet add package DannyGoodacre.Identity
```

> **Note:** The package is currently published to GitHub Packages only; publication to nuget.org is planned. See the [installation note](../DannyGoodacre.Cqrs/README.md#installation) for adding the feed.

## Setup

Derive your `DbContext` from `IdentityContext`:

```csharp
public sealed class BlackjackContext(DbContextOptions<BlackjackContext> options) : IdentityContext(options)
{
    public DbSet<Table> Tables => Set<Table>();
}
```

Then register, map, and initialize:

```csharp
builder.Services.AddDbContext<BlackjackContext>(o => o.UseSqlite(connectionString));

builder.Services.AddUsernameIdentity<BlackjackContext>(builder.Configuration);

builder.Services.AddDataProtection()
    .SetApplicationName("Blackjack")
    .PersistKeysToDbContext<BlackjackContext>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("identity").MapIdentityEndpoints();

app.MapPost("tables", (ClaimsPrincipal user) => ... user.GetUserId() ...)
    .RequireAuthorization(IdentityDefaults.AdminPolicy);

await app.Services.InitializeIdentityAsync();
await app.RunAsync();
```

- `AddUsernameIdentity<TContext>` registers Identity with the EF Core stores on `TContext`, cookie authentication, and the `Admin` authorization policy. It returns Identity's own `IdentityBuilder`. The name avoids a clash with Microsoft's `AddIdentity`.
- `AddDataProtection()...PersistKeysToDbContext<TContext>()` keeps sessions alive across restarts. See [Data Protection keys](#data-protection-keys).
- `InitializeIdentityAsync` prepares the database at startup. See [Startup initialization](#startup-initialization).

You own the migrations. Because your context derives from `IdentityContext`, they include Identity's tables (with their default `AspNet*` names), the `InviteCodes` table, and the `DataProtectionKeys` table. Apply them before `InitializeIdentityAsync` runs. If you override `OnModelCreating`, call `base.OnModelCreating` first.

### Configuration

The package has one option of its own, bound from the `Identity` configuration section. You can also pass a delegate as the second argument of `AddUsernameIdentity`, which runs after binding.

```json
{
  "Identity": {
    "InitialAdmin": { "Username": "admin", "Password": "..." },
    "SecurityStampValidationIntervalMinutes": 1
  }
}
```

| Option | Default | Purpose |
| --- | --- | --- |
| `InitialAdmin` | none | `Username`, `Password`, and `Email` of the first admin; `Email` is needed only when Identity requires a unique email. See [Startup initialization](#startup-initialization). |
| `SecurityStampValidationIntervalMinutes` | `1` | How often a signed-in user's cookie is checked against the database. See [How changes reach existing sessions](#how-changes-reach-existing-sessions). |

Configure everything else through ASP.NET Core Identity, **after** `AddUsernameIdentity`, so your settings win:

```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 10;
    options.User.RequireUniqueEmail = true;   // registration then requires an email
});

builder.Services.ConfigureApplicationCookie(options => options.ExpireTimeSpan = TimeSpan.FromDays(30));
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(4));
```

`AddUsernameIdentity` changes only these of Identity's defaults:

| Setting | Value | Why |
| --- | --- | --- |
| `SecurityStampValidatorOptions.ValidationInterval` | `SecurityStampValidationIntervalMinutes`, 1 minute by default (Identity's default is 30) | How quickly admin changes, disabling, and password changes reach existing sessions |
| Application cookie `OnRedirectToLogin`, `OnRedirectToAccessDenied` | 401 and 403 | These are API endpoints, so there is no login page to redirect to |

Everything else is Identity's default: no email required, lockout for five minutes after five failed sign-ins, reset tokens valid for one day, and Identity's password rules.

## Endpoints

All endpoints return `ProblemDetails`, or a validation problem keyed by Identity's error code, on failure. Status codes: 401 not signed in or bad credentials, 403 not an admin, a wrong invite code, or a disabled account, 404 unknown user, 409 conflicts (a taken username, [admin protection](#admin-protection), related data, two admins editing the same user at once), 429 locked out.

**Sessions** (`MapIdentitySessionEndpoints`)

| Method and path | Auth | Behavior |
| --- | --- | --- |
| `POST /sessions` | anonymous | `{ username, password, rememberMe }`. 204 and a cookie on success; 401 for wrong credentials; 403 for a disabled account; 429 when locked out, with `Retry-After`. `rememberMe` makes the cookie persistent. |
| `DELETE /sessions` | signed in | Signs out. 204. |
| `POST /password-reset` | anonymous | `{ username, token, newPassword }`. Sets the password and clears any lockout. 400 with the same body for an unknown username and for an invalid or expired token. |

**Users** (`MapIdentityUserEndpoints`)

| Method and path | Auth | Behavior |
| --- | --- | --- |
| `POST /users` | anonymous | `{ username, password, inviteCode, email? }`. 201 with `Location` and the new user, who can sign in straight away; 403 for a missing or wrong invite code; 409 if the username is taken. |
| `GET /users/me` | signed in | `{ id, username, email, isAdmin, isEnabled }`. 401, and the cookie is cleared, if the user no longer exists. |
| `PUT /users/me/password` | signed in | `{ currentPassword, newPassword }`. The caller stays signed in; their other sessions end. |
| `DELETE /users/{id}` | self, or admin | See [Disable rather than delete](#disable-rather-than-delete). Signs out when deleting yourself. |

**Admin** (`MapIdentityAdminEndpoints`; every endpoint requires an admin)

| Method and path | Behavior |
| --- | --- |
| `GET /users` | Every user, with `isAdmin` and `isEnabled` |
| `GET /users/{id}` | One user |
| `PUT /users/{id}/enabled` | Enables the account. Idempotent. |
| `DELETE /users/{id}/enabled` | Disables the account and ends its sessions. Idempotent. |
| `POST /users/{id}/password-reset` | `{ username, token }`. See [Password reset](#password-reset). |
| `PUT /users/{id}/admin`, `DELETE` | Makes the user an admin, or removes them. Idempotent. |
| `GET /invite-code` | `{ code }` |
| `POST /invite-code` | Replaces the invite code and returns the new one. Existing accounts are unaffected. |

### Mapping the groups separately

`MapIdentityEndpoints` maps all three groups under one parent group. To map only some, use different prefixes, or attach conventions, map each group yourself. Each method returns its `RouteGroupBuilder`:

```csharp
app.MapGroup("auth").MapIdentitySessionEndpoints().RequireRateLimiting("sign-in");
app.MapGroup("auth").MapIdentityUserEndpoints();
app.MapGroup("admin").MapIdentityAdminEndpoints();
```

- The groups do not depend on each other. A `Location` header is built from the request path, so `POST /auth/users` returns `/auth/users/{id}`, which is served by the admin group's `GET /users/{id}`. If you map the admin group under another prefix, the `Location` points to a path that is not mapped.
- Map each group once per prefix. Mapping a group twice under the same prefix fails at request time with an ambiguous match.

## Users and admins

An admin is a member of the `Admin` role (`IdentityDefaults.AdminRole`). Admins can do everything the admin endpoints offer; everyone else is a regular user. Use the `Admin` policy (`IdentityDefaults.AdminPolicy`) on your own admin-only endpoints and hubs:

```csharp
app.MapPost("tables", ...).RequireAuthorization(IdentityDefaults.AdminPolicy);

[Authorize(Policy = IdentityDefaults.AdminPolicy)]
public Task CloseTable(Guid tableId) { ... }
```

What a regular user can do inside your app, such as whose turn it is, belongs to your app's own logic, not to roles.

### Disabling accounts

Disabling uses Identity's own lockout: a disabled account is locked out until the year 9999, and its security stamp changes, so its sessions end at the next check. Enabling clears the lockout. Disabled accounts cannot sign in (403), and no reset token is issued for them.

### Admin protection

The app always keeps at least one enabled admin, so these return 409:

- Disabling, deleting, or removing the admin role from the last enabled admin
- Disabling your own account; an admin who wants to leave asks another admin

## Invite code

Registration requires the current invite code. `InitializeIdentityAsync` creates a random one on first run. Admins read it with `GET /invite-code` and replace it with `POST /invite-code`, for example when it has been shared too widely. Replacing it only affects registrations from then on.

## How changes reach existing sessions

The cookie is checked against the database every `SecurityStampValidationIntervalMinutes` (1 minute by default). The check runs while handling a signed-in user's request, so idle users cost nothing. At each check, the user's roles are reloaded, so:

- **Making a user an admin, or removing them,** reaches their existing sessions within the interval, **without** signing them out. Until then, their cookie still has the old role.
- **Disabling an account, changing a password, and resetting a password** change the user's security stamp, which **signs the user out** of every session at the next check. Changing your own password keeps the session that made the change.

## Password reset

There is no email, so a user who forgets their password gets back in through an admin:

1. The admin calls `POST /users/{id}/password-reset`, which returns the username and a token.
2. The admin passes the token on outside the app.
3. The user calls `POST /password-reset` with `{ username, token, newPassword }`. This also clears a lockout, because a locked-out user is the likely caller.

A token stops working when it expires (`DataProtectionTokenProviderOptions.TokenLifespan`, one day by default), once it has been used, once the password changes another way, or once the account is disabled. Issuing a second token does not invalidate the first. No token is issued for your own account (409; use `PUT /users/me/password`) or for a disabled account (409). Resetting the password ends the user's other sessions.

## Startup initialization

`InitializeIdentityAsync` runs in one transaction, inside the context's execution strategy:

1. Creates the `Admin` role if it is missing.
2. Creates the invite code if it is missing.
3. If `InitialAdmin` is set and no user has that username, creates the user and makes them an admin. Otherwise it does nothing, so you can remove the password from configuration after the first run. Without `InitialAdmin`, it throws if there is no enabled admin.

If any step fails, nothing is saved and it throws `InvalidOperationException`.

## Cookies

The cookie is Identity's application cookie, with Identity's defaults: `HttpOnly`, `SameSite=Lax`, secure when the request is, valid for 14 days with sliding expiration. Change them with `ConfigureApplicationCookie`. For an app served over HTTPS from the same site as its frontend, this is a sensible hardening:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

- A `rememberMe` sign-in makes the cookie persistent; otherwise it lasts for the browser session.
- A frontend on a different site needs `SameSite=None` and CORS with credentials, and then CSRF protection needs thought. Serving the frontend from the same site is simpler.
- SignalR hubs on the same site authenticate with the same cookie. `[Authorize]` and the `Admin` policy work on hubs unchanged, and `Context.UserIdentifier` is the user's id.
- The package calls `AddAuthentication` with Identity's application cookie as the default scheme. Calling `AddAuthentication()` again in your app keeps that default, unless you set a different default scheme yourself.

## Data Protection keys

ASP.NET Core encrypts the cookie and the reset tokens with Data Protection keys. By default the keys live in a folder on the machine, or only in memory, so in a container every redeploy signs every user out and invalidates every reset token. Store them somewhere that lasts. `IdentityContext` already has the `DataProtectionKeys` table, so the database is the easy choice:

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("Blackjack")
    .PersistKeysToDbContext<BlackjackContext>();
```

- Set the application name from the first deploy. Without it, the name comes from the app's content root path, so moving the app to another folder or host invalidates every cookie; adding or changing it later does the same once.
- Keys stored this way are **not encrypted at rest**. Anyone who can read the database can forge cookies; they could also read every password hash. To encrypt them, add `ProtectKeysWithCertificate` or another `ProtectKeysWith*` method.
- Any other key store (Azure Blob Storage, Redis, a mounted volume) works too; the table then stays empty.

## Disable rather than delete

Your tables will reference `User.Id`, so deleting a user either cascades through that data or fails. Disabling is the normal way to remove someone: it blocks sign-in, ends their sessions, keeps their data, and can be undone.

Deletion is for accounts with no history, such as mistakes. `DELETE /users/{id}` returns 409 if the database refuses the delete because of related data; disable the account instead.

Use `DeleteBehavior.Restrict` (or `NoAction`) on foreign keys to `User` for data worth keeping, so deletion fails loudly instead of cascading. Use `Cascade` only for data that should disappear with the user. The package's own tables (role memberships, user claims, logins, tokens) already cascade.

## Storing your own per-user data

`User` is sealed. Keep per-user data, such as a display name or a chip balance, in your own table keyed by `User.Id`:

```csharp
public sealed class Player
{
    public Guid UserId { get; init; }

    public string DisplayName { get; set; } = "";

    public int Chips { get; set; }
}

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    builder.Entity<Player>(player =>
    {
        player.HasKey(p => p.UserId);
        player.HasOne<User>().WithOne().HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    });
}
```

Read the signed-in user's id with `GetUserId()`:

```csharp
app.MapGet("players/me", async (ClaimsPrincipal user, BlackjackContext db) =>
    {
        Guid? userId = user.GetUserId();

        var player = await db.Set<Player>().AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId);

        return player is null ? Results.NotFound() : Results.Ok(player);
    })
    .RequireAuthorization();
```

## Keeping the package replaceable

To swap this package for another identity system later, keep your app's dependence on it to standard ASP.NET Core types:

- Read the current user from `ClaimsPrincipal`. `GetUserId()` only parses the `NameIdentifier` claim, which any cookie or token system can provide.
- Authorize admin-only actions with the policy name `"Admin"`, and everything else with `[Authorize]` or `RequireAuthorization()`. A replacement registers its own `"Admin"` policy.
- Key your own tables by the user's `Guid`. The foreign key to `AspNetUsers` is the only link to this package's schema, and removing it is one migration.

Package-specific code then appears only in `Program.cs` and your `DbContext`'s base class.
