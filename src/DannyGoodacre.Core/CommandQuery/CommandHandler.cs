using DannyGoodacre.Core.CommandQuery.Abstractions;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Core.CommandQuery;

public abstract class CommandHandler<TCommand>(ILogger logger)
    : CommandHandlerBase<TCommand, Result>(logger)
    where TCommand : ICommand
{
    protected private override Result MapResult(Result result)
        => result;
}

public abstract class CommandHandler<TCommand, TResult>(ILogger logger)
    : CommandHandlerBase<TCommand, Result<TResult>>(logger)
    where TCommand : ICommand
{
    protected private override Result<TResult> MapResult(Result result)
        => new(result);
}

public interface IDoThing
{
    Task<Result> ExecuteAsync(string value, CancellationToken cancellationToken = default);
}

internal sealed record DoThingCommand : ICommand
{
    public required string Value { get; init; }
}

internal sealed class DoThingHandler(ILogger logger, IService service) : CommandHandler<DoThingCommand>(logger)
{
    protected override string CommandName => "Do Thing";

    protected override void Validate(ValidationState validationState, DoThingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Value))
        {
            validationState.AddError(nameof(command.Value), "Must not be null, empty, or whitespace.");
        }
    }

    protected async override Task<Result> InternalExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    {
        bool hasWorked = await service.RunAsync(command.Value);

        return hasWorked
            ? Result.Success()
            : Result.DomainError("Has not worked");
    }

    // public new Task<Result> ExecuteAsync(DoThingCommand command, CancellationToken cancellationToken = default)
    //     => base.ExecuteAsync(command, cancellationToken);

    public Task<Result> ExecuteAsync(string value, CancellationToken cancellationToken = default)
        => ExecuteAsync(new DoThingCommand
        {
            Value = value
        }, cancellationToken);
}
