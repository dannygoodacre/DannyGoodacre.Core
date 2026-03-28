namespace DannyGoodacre.Core.CommandQuery.Abstractions;

/// <summary>
/// Defines an abstraction for persisting accumulated changes to the application state.
/// </summary>
public interface IStateUnit
{
    /// <summary>
    /// Persist all changes made during this operation to the application state.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
