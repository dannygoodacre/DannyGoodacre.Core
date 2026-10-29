using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace CqrsTest;

public sealed record CreateUserCommand(string Username, string Email)
    : ICommand, IStateCommand;

public sealed class CreateUserHandler(IUserRepository userRepository)
    : IValidator<CreateUserCommand>,
      ICommandHandler<CreateUserCommand, Guid>,
      IPostCommitHook<CreateUserCommand, Guid>
{
    public void Validate(CreateUserCommand command, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(command.Username))
        {
            validationState.AddError(nameof(command.Username), "Must not be null, empty, or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            validationState.AddError(nameof(command.Email), "Must not be null, empty, or whitespace.");
        }
    }

    public async Task<IResult<Guid>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = User.Create(command.Username, command.Email);

        userRepository.Add(user);

        return new Success<Guid>(user.PublicId);
    }

    public Task AfterSaveAsync(CreateUserCommand command, IResult<Guid> result, CancellationToken cancellationToken)
    {
        Console.WriteLine("Post commit called");

        return Task.CompletedTask;
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
