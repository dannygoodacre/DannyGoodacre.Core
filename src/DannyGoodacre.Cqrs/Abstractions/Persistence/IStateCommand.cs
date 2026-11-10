namespace DannyGoodacre.Cqrs;

/// <summary>
/// Marker interface indicating that a command mutates state and requires automatic unit of work persistence.
/// </summary>
public interface IStateCommand : ICommand;
