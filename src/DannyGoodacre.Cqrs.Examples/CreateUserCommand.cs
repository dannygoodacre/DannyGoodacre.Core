using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples;

public sealed record CreateUserCommand(string Email, string Name) : IStateCommand;

public sealed class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    public void Validate(CreateUserCommand request, ValidationState state)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            state.AddError(nameof(request.Email), "Email is required.");
        }
        else if (!request.Email.Contains('@'))
        {
            state.AddError(nameof(request.Email), "Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            state.AddError(nameof(request.Name), "Name is required.");
        }
    }
}

public sealed class CreateUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public Task<IResult<Guid>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var userId = Guid.NewGuid();

        userRepository.Add(new User(userId, command.Email, command.Name));

        return Task.FromResult<IResult<Guid>>(Result<Guid>.Success(userId));
    }
}

public sealed class SendWelcomeEmailPostCommitHook(IEmailService emailService)
    : IPostCommitHook<CreateUserCommand, Guid>
{
    public async Task AfterCommitAsync(
        CreateUserCommand command,
        IResult<Guid> result,
        CancellationToken cancellationToken = default)
        => await emailService.SendWelcomeEmailAsync(command.Email, cancellationToken);
}
