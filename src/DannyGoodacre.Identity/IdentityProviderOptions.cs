namespace DannyGoodacre.Identity;

/// <summary>
/// The package's own options, bound from the <c>Identity</c> configuration section. Everything else is configured
/// through ASP.NET Core Identity's own options.
/// </summary>
public sealed class IdentityProviderOptions
{
    public const string SectionName = "Identity";

    /// <summary>
    /// The account created by <see cref="ServiceProviderExtensions.InitializeIdentityAsync"/> when no user with
    /// this username exists. It can be removed from configuration after the first run.
    /// </summary>
    public InitialAdminOptions? InitialAdmin { get; set; }

    /// <summary>
    /// How often, in minutes, a signed-in user's cookie is checked against the database. Disabling, password changes,
    /// and admin changes reach existing sessions within this interval. Zero checks on every request.
    /// </summary>
    public int SecurityStampValidationIntervalMinutes { get; set; } = 1;
}

public sealed class InitialAdminOptions
{
    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>
    /// Required when <c>IdentityOptions.User.RequireUniqueEmail</c> is <see langword="true"/>.
    /// </summary>
    public string? Email { get; set; }
}
