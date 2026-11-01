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
        /// and returns the error message.
        /// </summary>
        /// <returns>The conflict message.</returns>
        public string ShouldBeConflict()
        {
            var conflict = Assert.IsType<Conflict>(result, exactMatch: false);

            return conflict.Message;
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="Conflict"/> or <see cref="Conflict{T}"/> outcome
        /// with the specified error message.
        /// </summary>
        /// <param name="expectedMessage">The expected conflict error message.</param>
        public void ShouldBeConflict(string expectedMessage)
        {
            string actualMessage = result.ShouldBeConflict();

            Assert.Equal(expectedMessage, actualMessage);
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="DomainError"/> or <see cref="DomainError{T}"/>
        /// outcome and returns the error message.
        /// </summary>
        /// <returns>The domain error message.</returns>
        public string ShouldBeDomainError()
        {
            var domainError = Assert.IsType<DomainError>(result, exactMatch: false);

            return domainError.Message;
        }

        /// <summary>
        /// Asserts that the result represents a <see cref="DomainError"/> or <see cref="DomainError{T}"/>
        /// outcome with the specified error message.
        /// </summary>
        /// <param name="expectedMessage">The expected domain error message.</param>
        public void ShouldBeDomainError(string expectedMessage)
        {
            string actualMessage = result.ShouldBeDomainError();

            Assert.Equal(expectedMessage, actualMessage);
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="InternalError"/> or <see cref="InternalError{T}"/>
        /// outcome and returns the underlying <see cref="Error"/> details.
        /// </summary>
        /// <returns>The internal error details.</returns>
        public Error ShouldBeInternalError()
        {
            var internalError = Assert.IsType<InternalError>(result, exactMatch: false);

            return internalError.Error;
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="InternalError"/> or <see cref="InternalError{T}"/>
        /// outcome matching the specified error details.
        /// </summary>
        /// <param name="expectedError">The expected <see cref="Error"/> struct value.</param>
        public void ShouldBeInternalError(Error expectedError)
        {
            Error actualError = result.ShouldBeInternalError();

            Assert.Equal(expectedError, actualError);
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="InternalError"/> or <see cref="InternalError{T}"/>
        /// outcome containing an exception matching <paramref name="expectedException"/>.
        /// </summary>
        /// <param name="expectedException">The expected exception instance.</param>
        public void ShouldBeInternalError(Exception expectedException)
        {
            Error actualError = result.ShouldBeInternalError();

            Assert.Equal(expectedException, actualError.Exception);
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="Invalid"/> outcome
        /// (or <see cref="Invalid{T}"/>) and returns the underlying <see cref="ValidationState"/>.
        /// </summary>
        /// <returns>The validation state containing validation errors.</returns>
        public ValidationState ShouldBeInvalid()
        {
            var invalid = Assert.IsType<Invalid>(result, exactMatch: false);

            return invalid.ValidationState;
        }

        /// <summary>
        /// Asserts that the result represents an <see cref="Invalid"/> outcome
        /// and matches the specified expected validation state.
        /// </summary>
        /// <param name="expectedValidationState">The expected validation state to match against.</param>
        public void ShouldBeInvalid(ValidationState expectedValidationState)
        {
            ValidationState actualState = result.ShouldBeInvalid();

            actualState.ShouldMatch(expectedValidationState);
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
