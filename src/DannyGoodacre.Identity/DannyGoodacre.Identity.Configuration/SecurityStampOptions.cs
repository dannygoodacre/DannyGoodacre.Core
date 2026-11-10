namespace DannyGoodacre.Identity.Configuration;

public sealed class SecurityStampOptions
{
    public TimeSpan ValidationInterval { get; set; } = TimeSpan.Parse("00:30:00");
}
