namespace DannyGoodacre.Cqrs;

/// <summary>
/// A query.
/// </summary>
public interface IQuery<out TResult> : IRequest where TResult : notnull;
