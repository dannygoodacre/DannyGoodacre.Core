using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal abstract class ValidationDecoratorBase<TRequest>(
    ILogger logger,
    IEnumerable<IValidator<TRequest>> validators)
    where TRequest : IRequest
{
    private static readonly string RequestName = TypeNameFormatter.Format(typeof(TRequest));

    private readonly IValidator<TRequest>[] _validators = [.. validators];

    protected async Task<IResult> ExecuteWithValidationAsync(
        TRequest request,
        Func<CancellationToken, Task<IResult>> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0)
        {
            return await next(cancellationToken);
        }

        var validationState = new ValidationState();

        foreach (IValidator<TRequest> validator in _validators)
        {
            validator.Validate(request, validationState);
        }

        if (!validationState.HasErrors)
        {
            return await next(cancellationToken);
        }

        logger.LogRequestValidationFailed(RequestName, validationState);

        return new Invalid(validationState);
    }
}

internal abstract class ValidationDecoratorBase<TRequest, TResult>(
    ILogger logger,
    IEnumerable<IValidator<TRequest>> validators)
    where TRequest : IRequest
    where TResult : notnull
{
    private static readonly string RequestName = TypeNameFormatter.Format(typeof(TRequest));

    private readonly IValidator<TRequest>[] _validators = [.. validators];

    protected async Task<IResult<TResult>> ExecuteWithValidationAsync(
        TRequest request,
        Func<CancellationToken, Task<IResult<TResult>>> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0)
        {
            return await next(cancellationToken);
        }

        var validationState = new ValidationState();

        foreach (IValidator<TRequest> validator in _validators)
        {
            validator.Validate(request, validationState);
        }

        if (!validationState.HasErrors)
        {
            return await next(cancellationToken);
        }

        logger.LogRequestValidationFailed(RequestName, validationState);

        return new Invalid<TResult>(validationState);
    }
}
