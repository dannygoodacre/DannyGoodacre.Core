using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Queries;

public interface IValidateSecurityStamp
{
    Task<Result<bool>> ExecuteAsync(string username, string securityStamp, CancellationToken cancellationToken = default);
}

internal sealed record ValidateSecurityStampQuery : IQuery
{
    public required string Username { get; init; }

    public required string SecurityStamp { get; init; }
}

internal sealed class ValidateSecurityStampHandler(ILogger<ValidateSecurityStampHandler> logger, IUserRepository repository)
    : QueryHandler<ValidateSecurityStampQuery, bool>(logger), IValidateSecurityStamp
{
    protected override string QueryName => "Validate Principal";

    protected override void Validate(ValidationState state, ValidateSecurityStampQuery query)
    {
        state.IsNotNullEmptyOrWhitespace(query.Username, nameof(query.Username));

        state.IsNotNullEmptyOrWhitespace(query.SecurityStamp, nameof(query.SecurityStamp));
    }

    protected async override Task<Result<bool>> InternalExecuteAsync(ValidateSecurityStampQuery query, CancellationToken cancellationToken = default)
    {
        User? user = await repository.GetAsync(query.Username, cancellationToken);

        return Success(user is not null && user.SecurityStamp != query.SecurityStamp);
    }

    public Task<Result<bool>> ExecuteAsync(string username, string securityStamp, CancellationToken cancellationToken = default)
        => ExecuteAsync(new ValidateSecurityStampQuery
        {
            Username = username,
            SecurityStamp = securityStamp
        }, cancellationToken);
}
