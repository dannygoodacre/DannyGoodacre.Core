using DannyGoodacre.Core.CommandQuery.Abstractions;

namespace DannyGoodacre.Core.Identity;

public interface IUnitOfWorkWithTransaction : IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
