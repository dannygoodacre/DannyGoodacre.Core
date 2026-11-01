namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines a query request.
/// </summary>
public interface IQuery<out TResult> : IRequest where TResult : notnull;
