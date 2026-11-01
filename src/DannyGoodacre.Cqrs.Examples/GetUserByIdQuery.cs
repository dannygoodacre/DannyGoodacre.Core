using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs.Examples;


public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;

public sealed class GetUserByIdQueryValidator : IValidator<GetUserByIdQuery>
{
    public void Validate(GetUserByIdQuery request, ValidationState state)
    {
        if (request.UserId == Guid.Empty)
        {
            state.AddError(nameof(request.UserId), "User ID cannot be empty.");
        }
    }
}

public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<IResult<UserResponse>> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        return user is null
            ? Result<UserResponse>.NotFound()
            : Result<UserResponse>.Success(user.ToResponse());
    }
}
