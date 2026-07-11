namespace DannyGoodacre.Identity.Configuration;

public sealed class PasswordValidatorOptions
{
    public bool RequiresLowercase { get; set; }

    public bool RequiresUppercase { get; set; }

    public bool RequireDigit { get; set; }

    public bool RequiresNonAlphanumeric { get; set; }

    public int MinimumLength { get; set; }
}
