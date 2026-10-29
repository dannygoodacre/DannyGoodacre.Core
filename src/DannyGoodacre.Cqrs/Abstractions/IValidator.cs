using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

public interface IValidator<in TRequest> where TRequest : IRequest
{
    void Validate(TRequest request, ValidationState validationState);
}
