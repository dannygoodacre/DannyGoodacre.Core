namespace DannyGoodacre.Core.Identity;

public interface ITransactionProvider
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
