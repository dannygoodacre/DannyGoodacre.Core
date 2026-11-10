namespace DannyGoodacre.Cqrs;

/// <summary>
/// Marker interface indicating that a command mutates state within an explicit database transaction boundary.
/// </summary>
public interface ITransactedStateCommand : ICommand;
