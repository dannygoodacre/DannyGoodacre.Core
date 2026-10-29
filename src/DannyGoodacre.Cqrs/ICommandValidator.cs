using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

public interface ICommandValidator<in TCommand> where TCommand : ICommand
{
    void Validate(TCommand command, ValidationState validationState);
}
