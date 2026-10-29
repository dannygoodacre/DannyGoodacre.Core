using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

public sealed class LoggingCommandHandlerDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<TCommand> logger) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<IResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        string commandName = typeof(TCommand).Name;

        try
        {
            logger.LogInformation("Command {CommandName} started", commandName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Command {CommandName} was canceled before execution", commandName);

                return new Canceled();
            }

            var result = await inner.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning("Command {CommandName} completed with failure result: {Result}", commandName, result);
            }
            else
            {
                logger.LogInformation("Command {CommandName} completed successfully", commandName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Command {CommandName} was canceled during execution", commandName);

            return new Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command {CommandName} threw an unhandled exception", commandName);

            return new InternalError(ex.Message);
        }
    }
}

public sealed class LoggingCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<TCommand> logger) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
{
    public async Task<IResult<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        string commandName = typeof(TCommand).Name;

        try
        {
            logger.LogInformation("Command {CommandName} started", commandName);

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Command {CommandName} was canceled before execution", commandName);

                return new Canceled<TResponse>();
            }

            var result = await inner.HandleAsync(command, cancellationToken);
            Console.WriteLine($"RESULT: {result}");

            if (!result.IsSuccess)
            {
                logger.LogWarning("Command {CommandName} completed with failure result", commandName);
            }
            else
            {
                logger.LogInformation("Command {CommandName} completed successfully", commandName);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Command {CommandName} was canceled during execution", commandName);

            return new Canceled<TResponse>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command {CommandName} threw an unhandled exception", commandName);

            return new InternalError<TResponse>(ex.Message);
        }
    }
}
