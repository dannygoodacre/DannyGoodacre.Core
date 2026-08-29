namespace DannyGoodacre.Primitives;

public static class TypedResultFunctionalExtensions
{
    extension<T>(IResult<T> result)
    {
        /// <summary>
        /// Chain a subsequent operation that accepts the unwrapped payload and produces a new <see cref="IResult{T}"/>.
        /// If the result is unsuccessful, short-circuit and return the result.
        /// </summary>
        /// <param name="function">A transformation delegate</param>
        /// <typeparam name="TOut">The payload type</typeparam>
        /// <returns>A <see cref="IResult{T}"/> instance</returns>
        public IResult<TOut> Bind<TOut>(Func<T, IResult<TOut>> function)
            => result is Success<T> success
                ? function(success.Value)
                : result.MapFailure<TOut>();

        public IResult<T> Ensure(Func<T, bool> predicate, Func<T, IResult<T>> errorFactory)
            => result is not Success<T> success
                ? result
                : predicate(success.Value)
                    ? result
                    : errorFactory(success.Value);

        public T? GetValueOrDefault()
            => result is Success<T> success
                ? success.Value
                : default;

        public IResult<TOut> Map<TOut>(Func<T, TOut> function)
            => result is Success<T> success
                ? new Success<TOut>(function(success.Value))
                : result.MapFailure<TOut>();

        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IResult, TOut> onFailure)
            => result is Success<T> success
                ? onSuccess(success.Value)
                : onFailure(result);

        public IResult<T> OrElse(Func<IResult<T>> function)
            => result is Success
                ? result
                : function();

        public IResult<T> Tap(Action<T> func)
        {
            if (result is Success<T> success)
            {
                func(success.Value);
            }

            return result;
        }

        public IResult<T> TapFailure(Action action)
        {
            if (result is not Success<T>)
            {
                action();
            }

            return result;
        }

        public IResult<T> TapFailure(Action<IResult> action)
        {
            if (result is not Success<T>)
            {
                action(result);
            }

            return result;
        }}
}
