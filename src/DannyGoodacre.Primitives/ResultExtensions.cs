// namespace DannyGoodacre.Primitives;
//
// public static class ResultExtensions
// {
//     extension<T>(Result<T> result)
//     {
//         public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> f)
//             => result.IsSuccess
//                 ? f(result.Value)
//                 : result.MapFailure<TOut>();
//
//         public Result<TOut> Map<TOut>(Func<T, TOut> f)
//             => result.IsSuccess
//                 ? Result<TOut>.Success(f(result.Value))
//                 : result.MapFailure<TOut>();
//
//         public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Status, Error?, TOut> onFailure)
//             => result.IsSuccess
//                 ? onSuccess(result.Value)
//                 : onFailure(result.Status, result.Error);
//     }
//
//     extension(Result)
//     {
//         public static Result<T> Unit<T>(T value)
//             => Result.Success(value);
//     }
// }
