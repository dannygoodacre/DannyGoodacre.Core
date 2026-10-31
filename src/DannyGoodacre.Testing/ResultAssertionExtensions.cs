using DannyGoodacre.Primitives;
using Xunit;

namespace DannyGoodacre.Testing;

/// <summary>
/// Provides fluent xUnit assertion extension methods for <see cref="IResult"/> and <see cref="IResult{T}"/>.
/// </summary>
public static class ResultAssertionExtensions
{
    extension(IResult result)
    {
        /// <summary>
        /// Asserts that the result represents a successful outcome (<see cref="ISuccessResult"/>).
        /// </summary>
        public void ShouldBeSuccess()
        {
            Assert.True(result.IsSuccess, $"Expected Success, but got '{result.GetType().Name}'.");
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="Canceled"/> or <see cref="Canceled{T}"/> outcome.
        /// </summary>
        public void ShouldBeCanceled()
        {
            Assert.IsType<Canceled>(result, exactMatch: false);
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="Conflict"/> or <see cref="Conflict{T}"/> outcome
        /// with the specified error message.
        /// </summary>
        /// <param name="expectedMessage">The expected conflict error message.</param>
        public void ShouldBeConflict(string expectedMessage)
        {
            var conflict = Assert.IsType<Conflict>(result, exactMatch: false);

            Assert.Equal(expectedMessage, conflict.Message);
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="DomainError"/> or <see cref="DomainError{T}"/>
        /// outcome with the specified error message.
        /// </summary>
        /// <param name="expectedMessage">The expected domain error message.</param>
        public void ShouldBeDomainError(string expectedMessage)
        {
            var domainError = Assert.IsType<DomainError>(result, exactMatch: false);

            Assert.Equal(expectedMessage, domainError.Message);
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="InternalError"/> or <see cref="InternalError{T}"/>
        /// outcome matching the specified error details.
        /// </summary>
        /// <param name="expectedError">The expected <see cref="Error"/> struct value.</param>
        public void ShouldBeInternalError(Error expectedError)
        {
            var internalError = Assert.IsType<InternalError>(result, exactMatch: false);

            Assert.Equal(expectedError, internalError.Error);
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="Invalid"/> or <see cref="Invalid{T}"/> outcome
        /// matching the specified validation state.
        /// </summary>
        /// <param name="expectedValidationState">The expected validation state to match against.</param>
        public void ShouldBeInvalid(ValidationState expectedValidationState)
        {
            var invalid = Assert.IsType<Invalid>(result, exactMatch: false);

            invalid.ValidationState.ShouldMatch(expectedValidationState);
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="NotFound"/> or <see cref="NotFound{T}"/> outcome.
        /// </summary>
        public void ShouldBeNotFound()
        {
            Assert.IsType<NotFound>(result, exactMatch: false);
        }
    }

    extension<T>(IResult<T> result)
    {
        /// <summary>
        /// Asserts that the result is a <see cref="Success{T}"/> outcome and returns its payload value.
        /// </summary>
        /// <returns>The payload value contained within the successful result.</returns>
        public T ShouldBeSuccess()
        {
            var success = Assert.IsType<Success<T>>(result);

            return success.Value;
        }

        /// <summary>
        /// Asserts that the result is a <see cref="Success{T}"/> outcome and that its payload equals
        /// <paramref name="expectedValue"/>.
        /// </summary>
        /// <param name="expectedValue">The expected payload value.</param>
        public void ShouldBeSuccess(T expectedValue)
        {
            T actualValue = result.ShouldBeSuccess();

            Assert.Equal(expectedValue, actualValue);
        }
    }
}
