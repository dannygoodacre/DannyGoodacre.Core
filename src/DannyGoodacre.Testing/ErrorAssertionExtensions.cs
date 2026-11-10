using DannyGoodacre.Primitives;
using Xunit;

namespace DannyGoodacre.Testing;

/// <summary>
/// Contains fluent xUnit assertion extension methods for <see cref="Error"/>.
/// </summary>
public static class ErrorAssertionExtensions
{
    extension(Error error)
    {
        /// <summary>
        /// Asserts that the error message matches the expected value.
        /// </summary>
        public void ShouldHaveMessage(string expectedMessage)
        {
            Assert.Equal(expectedMessage, error.Message);
        }

        /// <summary>
        /// Asserts that the error code matches the expected code.
        /// </summary>
        public void ShouldHaveCode(string expectedCode)
        {
            Assert.Equal(expectedCode, error.Code);
        }

        /// <summary>
        /// Asserts that the error contains an exception of type <typeparamref name="TException"/>
        /// and returns it for further assertions.
        /// </summary>
        public TException ShouldHaveException<TException>()
            where TException : Exception
        {
            Assert.NotNull(error.Exception);
            return Assert.IsAssignableFrom<TException>(error.Exception);
        }

        /// <summary>
        /// Asserts that the error matches both the expected message and code.
        /// </summary>
        public void ShouldMatch(string expectedMessage, string expectedCode = "General")
        {
            Assert.Equal(expectedCode, error.Code);
            Assert.Equal(expectedMessage, error.Message);
        }

        /// <summary>
        /// Asserts that two <see cref="Error"/> struct instances match in message, code, and causal exception.
        /// </summary>
        public void ShouldMatch(Error expected)
        {
            Assert.Equal(expected.Code, error.Code);
            Assert.Equal(expected.Message, error.Message);
            Assert.Equal(expected.Exception, error.Exception);
        }
    }
}
