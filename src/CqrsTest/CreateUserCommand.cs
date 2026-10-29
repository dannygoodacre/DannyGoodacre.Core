using System.ComponentModel;
using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace CqrsTest;

// 1. The Command
public sealed record CreateUserCommand(string Username, string Email)
    : ICommand, IStateCommand;

// public sealed class CreateUserCommandValidator : ICommandValidator<CreateUserCommand>
// {
//     public void Validate(CreateUserCommand command, ValidationState validationState)
//     {
//         if (string.IsNullOrWhiteSpace(command.Username))
//         {
//             validationState.AddError(nameof(command.Username), "Must not be null, empty, or whitespace.");
//         }
//
//         if (string.IsNullOrWhiteSpace(command.Email))
//         {
//             validationState.AddError(nameof(command.Email), "Must not be null, empty, or whitespace.");
//         }
//     }
// }

// 2. The Handler (Zero base classes, zero factory boilerplate)
public sealed class CreateUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<CreateUserCommand>
{
    public async Task<IResult> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = User.Create(command.Username, command.Email);

        userRepository.Add(user);

        Error domainError = new("User with ID 42 was not found.");

        return new InternalError(domainError);

        // return new Success<Guid>(user.PublicId);
        return new Success();
    }
}

public interface IUserRepository
{
    void Add(User user);
}

public sealed class UserRepository : IUserRepository
{

    public void Add(User user)
    {
        Console.WriteLine("User added");
    }
}

public class User
{
    public static User Create(string name, string email) => new();

    public Guid PublicId { get; set; }
}

public class StateUnit : IStateUnit
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("SaveChangesAsync called");

        return Task.FromResult(0);
    }
}
