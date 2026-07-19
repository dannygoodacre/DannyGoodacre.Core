using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface ISeedSuperUser
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed class SeedSuperUserCommand : ICommand
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}

internal sealed class SeedSuperUserHandler(ILogger<SeedSuperUserHandler> logger,
                                           IStateUnit stateUnit,
                                           IHashingService hashingService,
                                           IUserRepository userRepository,
                                           IClaimRepository claimRepository,
                                           IUserClaimRepository userClaimRepository)
    : StateCommandHandler<SeedSuperUserCommand>(logger, stateUnit)
{

    protected override string CommandName => "Seed Super User";

    protected async override Task<Result> InternalExecuteAsync(SeedSuperUserCommand command, CancellationToken cancellationToken = default)
    {
        bool isUsernameTaken = await userRepository.ExistsAsync(command.Username, cancellationToken);

        if (isUsernameTaken)
        {
            return Conflict("Username already taken");
        }

        User user = userRepository.Add(new User
        {
            PublicId = Guid.NewGuid(),
            Username = command.Username,
            IsApproved = false,
            PasswordHash = hashingService.Hash(command.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });

        List<Claim> claims = await claimRepository.GetAllAsync(cancellationToken);

        foreach (Claim claim in claims)
        {
            _ = userClaimRepository.Add(new UserClaim
            {
                User = user,
                Claim = claim
            });
        }

        return Success();
    }
}
