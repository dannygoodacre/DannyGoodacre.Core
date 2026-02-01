using DannyGoodacre.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace DannyGoodacre.Identity.Extensions;

public static class ResultExtensions
{
    extension(Result result)
    {
        public IResult ToUnsuccessfulHttpResponse()
            => result.Status switch
            {
                Status.Invalid => result.ToValidationProblem(),

                Status.Failed => result.ToProblem(),

                Status.Cancelled => Results.StatusCode(499),

                Status.NotFound => Results.NotFound(),

                _ => Results.InternalServerError()
            };

        private IResult ToValidationProblem()
            => result.Status == Status.Invalid
                ? Results.ValidationProblem(result.ValidationState!.Errors.ToDictionary(x => x.Key, x => x.Value.ToArray()))
                : throw new ArgumentException($"Invalid status '{result.Status}'.");

        private IResult ToProblem()
            => result.Status == Status.Failed
                ? Results.Problem(statusCode: 400, title: result.Error)
                : throw new ArgumentException($"Invalid status '{result.Status}'.");
    }

    extension<T>(Result<T> result)
    {
        public IResult ToUnsuccessfulHttpResponse()
            => result.Status switch
            {
                Status.Invalid => result.ToValidationProblem(),

                Status.Failed => result.ToProblem(),

                Status.Cancelled => Results.StatusCode(499),

                Status.NotFound => Results.NotFound(),

                _ => Results.InternalServerError()
            };

        private IResult ToValidationProblem()
            => Results.ValidationProblem(result.ValidationState!.Errors.ToDictionary(x => x.Key, x => x.Value.ToArray()));

        private IResult ToProblem()
            => Results.Problem(statusCode: 400, title: result.Error);
    }
}
