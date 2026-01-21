namespace DannyGoodacre.Identity.Model;

public sealed record LoginRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}
