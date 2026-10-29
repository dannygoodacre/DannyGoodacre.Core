using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public sealed class ValidationCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<TCommand> logger,
    ICommandValidator<TCommand>? validator = null) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (validator is null)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        var validationState = new ValidationState();

        validator.Validate(command, validationState);

        if (!validationState.HasErrors)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        logger.LogCommandFailedValidation(typeof(TCommand).Name, validationState);

        return new Invalid(validationState);

    }
}

public sealed class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<TCommand> logger,
    ICommandValidator<TCommand>? validator = null) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : IResult
{
    public async Task<IResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (validator is null)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        var validationState = new ValidationState();

        validator.Validate(command, validationState);

        if (!validationState.HasErrors)
        {
            return await inner.HandleAsync(command, cancellationToken);
        }

        logger.LogCommandFailedValidation(typeof(TCommand).Name, validationState);

        return new Invalid<TResult>(validationState);

    }
}
