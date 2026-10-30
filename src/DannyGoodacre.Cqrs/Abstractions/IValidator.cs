using DannyGoodacre.Primitives;

namespace DannyGoodacre.Cqrs;

/// <summary>
/// Defines input validation rules for an incoming <see cref="IRequest"/> prior to pipeline execution.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
public interface IValidator<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// Validates the state of the request and records errors into <paramref name="validationState"/>.
    /// </summary>
    /// <param name="request">The incoming request instance to validate.</param>
    /// <param name="validationState">The container used to accumulate validation error messages.</param>
    void Validate(TRequest request, ValidationState validationState);
}
