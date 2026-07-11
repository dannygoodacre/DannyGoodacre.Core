using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DannyGoodacre.Identity;

internal static class ResultExtensions
{
    public static IResult ToHttpResponse(this Result result)
        => result.Status switch
        {
            Status.Success => Results.NoContent(),

            Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),

            Status.Conflict => Results.Conflict(result.Error),

            Status.Canceled => Results.BadRequest(new ProblemDetails
            {
                Title = "Request Canceled",
                Detail = "The operation was aborted by the client or timed out."
            }),

            Status.DomainError => Results.UnprocessableEntity(result.Error),

            Status.NotFound => Results.NotFound(),

            _ => Results.StatusCode(500)
        };

    public static IResult ToHttpResponse<T>(this Result<T> result)
        => result.Status switch
        {
            Status.Success => Results.Ok(result.Value),

            Status.Invalid => Results.BadRequest(result.ValidationState!.ToValidationProblemDetails()),

            Status.Conflict => Results.Conflict(result.Error),

            Status.Canceled => Results.BadRequest(new ProblemDetails
            {
                Title = "Request Canceled",
                Detail = "The operation was aborted by the client or timed out."
            }),

            Status.DomainError => Results.UnprocessableEntity(result.Error),

            Status.NotFound => Results.NotFound(),

            _ => Results.StatusCode(500)
        };
}
