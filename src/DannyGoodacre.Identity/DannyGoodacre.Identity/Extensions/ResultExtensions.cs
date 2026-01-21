using DannyGoodacre.Core;
using DannyGoodacre.Identity.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

// ReSharper disable once CheckNamespace
namespace DannyGoodacre.Identity;

internal static class ResultExtensions
{
    extension(IdentityResult result)
    {
        public Result ToResult()
            => result.Succeeded
                ? Result.Success()
                : Result.InternalError(result.ToString());
    }

    extension(SignInResult result)
    {
        public Result ToResult()
            => result.Succeeded
                ? Result.Success()
                : Result.InternalError(result.ToString());
    }

    extension(Result result)
    {
        public IResult ToHttpResponse()
            => result.Status switch
            {
                Status.Success => Results.Ok(),
                Status.Invalid => Results.BadRequest((object?)result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.BadRequest((object?)result.Error),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.BadRequest("The request was cancelled."),
                _ => Results.InternalServerError(),
            };
    }
}
