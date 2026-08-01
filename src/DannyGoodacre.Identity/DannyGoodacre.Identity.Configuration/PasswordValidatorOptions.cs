namespace DannyGoodacre.Identity.Configuration;

public sealed class PasswordValidatorOptions
{
    public bool RequireLowercase { get; set; }

    public bool RequireUppercase { get; set; }

    public bool RequireDigit { get; set; }

    public bool RequireNonAlphanumeric { get; set; }

    public int MinimumLength { get; set; }
}
