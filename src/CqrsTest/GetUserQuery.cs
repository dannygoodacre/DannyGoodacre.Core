using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace CqrsTest;

public sealed record GetUserQuery(string Username) : IQuery<string>;

public sealed class GetUserQueryValidator : IValidator<GetUserQuery>
{
    public void Validate(GetUserQuery request, ValidationState validationState)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            validationState.AddError(nameof(request.Username), "Must not be null, empty, or whitespace.");
        }
    }
}

public sealed class GetUserQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserQuery, string>
{
    public async Task<IResult<string>> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Query running");

        return new Success<string>("test result");
    }
}
