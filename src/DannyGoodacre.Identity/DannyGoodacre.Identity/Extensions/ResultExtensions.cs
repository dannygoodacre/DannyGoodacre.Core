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
                : Result.DomainError(result.ToString());
    }

    extension(SignInResult result)
    {
        public Result ToResult()
            => result.Succeeded
                ? Result.Success()
                : Result.DomainError(result.ToString());
    }

    extension(Result result)
    {
        public IResult ToHttpResponse()
            => result.Status switch
            {
                Status.Success => Results.NoContent(),
                Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.Problem(result.Error, statusCode: 400),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.StatusCode(499),
                _ => Results.InternalServerError(),
            };
    }

    extension<T>(Result<T> result)
    {
        public IResult ToHttpResponse()
            => result.Status switch
            {
                Status.Success => Results.NoContent(),
                Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.Problem(result.Error, statusCode: 400),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.StatusCode(499),
                _ => Results.InternalServerError(),
            };

        public IResult ToCreationHttpResponse()
            => result.Status switch
            {
                Status.Success => Results.Created(),
                Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.Problem(result.Error, statusCode: 400),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.StatusCode(499),
                _ => Results.InternalServerError(),
            };

        private IResult ToUnsuccessfulHttpResponse()
            => result.Status switch
            {
                Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.Problem(result.Error, statusCode: 400),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.StatusCode(499),
                _ => Results.InternalServerError(),
            };
    }
}
