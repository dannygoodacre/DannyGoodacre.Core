namespace DannyGoodacre.Primitives;

public static class ResultFunctionalExtensions
{
    extension(Result)
    {
        /// <summary>
        /// Elevate the given value into a Result.
        /// </summary>
        /// <param name="value">The value to elevate.</param>
        /// <returns>An <see cref="IResult{T}"/> containing the given value.</returns>
        public static IResult<T> Unit<T>(T value)
            => Result.Success(value);
    }

    extension(IResult result)
    {
        public IResult Bind(Func<IResult> function)
            => result is Success
                ? function()
                : result;

        /// <summary>
        /// Chain a subsequent operation that produces an <see cref="IResult{T}"/>.
        /// </summary>
        /// <param name="function"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IResult<T> Bind<T>(Func<IResult<T>> function)
            => result is Success
                ? function()
                : result.MapFailure<T>();

        public T Match<T>(Func<T> onSuccess, Func<IResult, T> onFailure)
            => result is Success
                ? onSuccess()
                : onFailure(result);

        public IResult OrElse(Func<IResult> function)
            => result is Success
                ? result
                : function();

        public IResult Tap(Action action)
        {
            if (result is Success)
            {
                action();
            }

            return result;
        }

        public IResult TapFailure(Action action)
        {
            if (result is not Success)
            {
                action();
            }

            return result;
        }

        public IResult TapFailure(Action<IResult> action)
        {
            if (result is not Success)
            {
                action(result);
            }

            return result;
        }
    }
}
