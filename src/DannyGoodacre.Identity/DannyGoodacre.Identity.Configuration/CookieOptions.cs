using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity.Configuration;

public class CookieOptions
{
    public string Name { get; set; } = "DannyGoodacre.Identity";

    public bool HttpOnly { get; set; } = true;

    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    public PathString LoginPath { get; set; } = "/sessions";

    public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.Parse("01:00:00");

    public bool SlidingExpiration { get; set; } = true;

    public bool IsPersistent { get; set; } = true;
}
