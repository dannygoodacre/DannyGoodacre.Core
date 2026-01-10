namespace DannyGoodacre.Core.CommandQuery.Abstractions;

public interface ITransactionProvider
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
