namespace DannyGoodacre.Identity.Configuration;

public sealed class PasswordValidatorOptions
{
    public bool RequireLowercase { get; set; } = true;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireDigit { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; } = true;

    public int MinimumLength { get; set; } = 8;
}
