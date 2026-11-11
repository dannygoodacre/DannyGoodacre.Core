using Microsoft.AspNetCore.Identity;

namespace DannyGoodacre.Identity;

/// <summary>
/// An account. Keep your own per-user data in your own table, keyed by <see cref="IdentityUser{TKey}.Id"/>.
/// </summary>
public sealed class User : IdentityUser<Guid>;
