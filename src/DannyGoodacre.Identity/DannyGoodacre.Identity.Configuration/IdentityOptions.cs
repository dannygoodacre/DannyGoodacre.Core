namespace DannyGoodacre.Identity.Configuration;

public class IdentityOptions
{
   public CookieOptions Cookie { get; set; } = new();

   public PasswordValidatorOptions PasswordValidator { get; set; } = new();

   public SecurityStampOptions SecurityStamp { get; set; } = new();
}
