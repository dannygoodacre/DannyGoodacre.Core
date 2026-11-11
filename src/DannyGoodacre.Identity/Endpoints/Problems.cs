using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DannyGoodacre.Identity.Endpoints;

internal static class Problems
{
    private static readonly HashSet<string> ConflictCodes =
    [
        nameof(IdentityErrorDescriber.ConcurrencyFailure),
        nameof(IdentityErrorDescriber.DuplicateUserName),
        nameof(IdentityErrorDescriber.DuplicateEmail)
    ];

    /// <summary>
    /// Maps a failed <see cref="IdentityResult"/> to a 409 problem for duplicates and concurrent edits, and to a
    /// validation problem keyed by <see cref="IdentityError.Code"/> otherwise.
    /// </summary>
    public static IResult From(IdentityResult result)
    {
        if (result.Errors.FirstOrDefault(error => ConflictCodes.Contains(error.Code)) is { } conflict)
        {
            return Conflict(conflict.Description);
        }

        return Results.ValidationProblem(result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
    }

    public static IResult? Required(params (string Name, string? Value)[] fields)
    {
        var errors = fields
            .Where(field => string.IsNullOrWhiteSpace(field.Value))
            .ToDictionary(field => field.Name, field => new[] { $"{field.Name} is required." });

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    public static IResult Unauthorized(string detail)
        => Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: detail);

    public static IResult Forbidden(string detail)
        => Results.Problem(statusCode: StatusCodes.Status403Forbidden, detail: detail);

    public static IResult NotFound(string detail)
        => Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: detail);

    public static IResult Conflict(string detail)
        => Results.Problem(statusCode: StatusCodes.Status409Conflict, detail: detail);

    public static IResult UserNotFound()
        => NotFound("The user does not exist.");

    public static IResult LastAdmin()
        => Conflict("This would leave no enabled admin.");
}
