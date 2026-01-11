namespace DannyGoodacre.Core.Identity.Model;

public sealed record RegistrationRequest
{
    public required string Username  { get; init; }

    public required string Password { get; init; }
}
