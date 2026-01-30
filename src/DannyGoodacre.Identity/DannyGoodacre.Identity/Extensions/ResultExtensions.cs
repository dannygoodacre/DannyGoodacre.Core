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
        {
            if (result.Succeeded)
            {
                return Result.Success();
            }

            if (result.Errors.Any(x => x.Code.Property == "General"))
            {
                return Result.DomainError(result.ToString());
            }

            var state = new ValidationState();

            foreach (var error in result.Errors)
            {
                var property = error.Code switch
                {
                    var x when x.Contains("UserName") => "Username",
                    var x when x.Contains("Password") => "Password",
                    var x when x.Contains("User") => "User",
                    _ => "General"
                };

                state.AddError(property, error.Description);
            }

            return Result.Invalid(state);
        }
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
                Status.Invalid => Results.ValidationProblem(result.ValidationState!.Errors.ToDictionary(x => x.Key, x => x.Value.ToArray())),
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
                Status.Success => Results.Ok(result.Value),
                Status.Invalid => Results.ValidationProblem(result.ValidationState!.Errors.ToDictionary(x => x.Key, x => x.Value.ToArray())),
                Status.DomainError => Results.Problem(result.Error, statusCode: 400),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.StatusCode(499),
                _ => Results.InternalServerError(),
            };
    }
}
