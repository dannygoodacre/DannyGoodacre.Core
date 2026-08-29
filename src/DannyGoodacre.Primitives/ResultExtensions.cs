namespace DannyGoodacre.Primitives;

public static class ResultExtensions
{
    extension<TOut>(IResult result)
    {
        public IResult<TOut> MapFailure()
            => result switch
            {
                Canceled => new Canceled<TOut>(),

                Conflict conflict => new Conflict<TOut>(conflict.Error),

                DomainError domainError => new DomainError<TOut>(domainError.Error),

                InternalError internalError => new InternalError<TOut>(internalError.Error),

                Invalid invalid => new Invalid<TOut>(invalid.ValidationState),

                NotFound => new NotFound<TOut>(),

                Success => throw new InvalidOperationException("Cannot map a successful result to a failure."),

                _ => throw new ArgumentOutOfRangeException(nameof(result))
            };
    }
}
