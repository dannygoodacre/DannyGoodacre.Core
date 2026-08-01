using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity.Configuration;

public class CookieOptions
{
    public string Name { get; set; } = "DannyGoodacre.Identity";

    public bool HttpOnly { get; set; } = true;

    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    public PathString LoginPath { get; set; } = "/session";

    public int ExpireTimeSpanInMinutes { get; set; } = 1440;

    public bool SlidingExpiration { get; set; } = true;

    public bool IsPersistent { get; set; } = true;
}
