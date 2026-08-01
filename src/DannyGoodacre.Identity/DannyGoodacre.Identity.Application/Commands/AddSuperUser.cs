using DannyGoodacre.Cqrs;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Commands;

public interface IAddSuperUser
{
    Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default);
}

internal sealed class AddSuperUserHandler(ILogger<AddSuperUserHandler> logger,
                                          IStateUnit stateUnit,
                                          IPasswordHashingService hashingService,
                                          IUserRepository userRepository,
                                          IClaimRepository claimRepository,
                                          IUserClaimRepository userClaimRepository)
    : StateCommandHandler<AddUserCommand>(logger, stateUnit), IAddSuperUser
{

    protected override string CommandName => "Add Super User";

    protected async override Task<Result> InternalExecuteAsync(AddUserCommand command, CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetWithTrackingAsync(command.Username, cancellationToken);

        if (user is null)
        {
            string passwordHash = hashingService.Hash(command.Password);

            user = userRepository.Add(User.CreateNew(command.Username, passwordHash, isApproved: true));
        }

        HashSet<int> existingClaimIds = await userClaimRepository.GetClaimIdsAsync(user.Id, cancellationToken);

        List<Claim> claims = await claimRepository.GetAllAsync(cancellationToken);

        foreach (Claim claim in claims.Where(x => !existingClaimIds.Contains(x.Id)))
        {
            _ = userClaimRepository.Add(new UserClaim
            {
                User = user,
                Claim = claim
            });
        }

        return Success();
    }

    public Task<Result> ExecuteAsync(string username, string password, CancellationToken cancellationToken = default)
        => ExecuteAsync(new AddUserCommand
        {
            Username = username,
            Password = password
        }, cancellationToken);
}
