namespace DannyGoodacre.Cqrs.Examples;

public sealed record User(Guid Id, string Email, string Name);

public sealed record UserResponse(Guid Id, string Email, string Name);

public static class UserExtensions
{
    public static UserResponse ToResponse(this User user)
        => new UserResponse(user.Id, user.Email, user.Name);
}
