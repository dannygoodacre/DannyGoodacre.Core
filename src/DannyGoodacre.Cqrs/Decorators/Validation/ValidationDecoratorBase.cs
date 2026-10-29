using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Cqrs;

internal abstract class ValidationDecoratorBase<TRequest>(
    ILogger logger,
    IValidator<TRequest>? validator = null)
    where TRequest : IRequest
{
    protected async Task<IResult> ExecuteWithValidationAsync(
        TRequest request,
        Func<CancellationToken, Task<IResult>> next,
        CancellationToken cancellationToken)
    {
        if (validator is null)
        {
            return await next(cancellationToken);
        }

        var validationState = new ValidationState();

        validator.Validate(request, validationState);

        if (!validationState.HasErrors)
        {
            return await next(cancellationToken);
        }

        logger.LogCommandFailedValidation(typeof(TRequest).Name, validationState);

        return new Invalid(validationState);
    }
}

internal abstract class ValidationDecoratorBase<TRequest, TResult>(
    ILogger logger,
    IValidator<TRequest>? validator = null)
    where TRequest : IRequest
    where TResult : notnull
{
    protected async Task<IResult<TResult>> ExecuteWithValidationAsync(
        TRequest request,
        Func<CancellationToken, Task<IResult<TResult>>> next,
        CancellationToken cancellationToken)
    {
        if (validator is null)
        {
            return await next(cancellationToken);
        }

        var validationState = new ValidationState();

        validator.Validate(request, validationState);

        if (!validationState.HasErrors)
        {
            return await next(cancellationToken);
        }

        logger.LogCommandFailedValidation(typeof(TRequest).Name, validationState);

        return new Invalid<TResult>(validationState);
    }
}
