using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Core.Identity;

internal static class ResultExtensions
{
    public static IResult ToHttpResponse(this Result result)
        => result.Status switch
            {
                Status.Success => Results.Ok(),
                Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.BadRequest(result.Error),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.BadRequest("The request was cancelled."),
                Status.InternalError => Results.InternalServerError(),
                _ => Results.InternalServerError(),
            };

    public static IResult ToHttpResponse<T>(this Result<T> result)
        => result.Status switch
            {
                Status.Success => Results.Ok(result.Value),
                Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),
                Status.DomainError => Results.BadRequest(result.Error),
                Status.NotFound => Results.NotFound(),
                Status.Cancelled => Results.BadRequest("The request was cancelled."),
                Status.InternalError => Results.InternalServerError(),
                _ => Results.InternalServerError()
            };
}
