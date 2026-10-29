using DannyGoodacre.Cqrs;
using DannyGoodacre.Primitives;

namespace CqrsTest;

public sealed record GetUserQuery(string Username) : IQuery<string>;

public sealed class GetUserQueryValidator : IValidator<GetUserQuery>
{
    public void Validate(GetUserQuery request, ValidationState validationState) { }
}

public sealed class GetUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserQuery, string>
{
    public async Task<IResult<string>> QueryAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        return new Success<string>("test result");
    }
}
