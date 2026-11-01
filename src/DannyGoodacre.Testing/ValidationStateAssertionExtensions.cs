using DannyGoodacre.Primitives;
using Xunit;

namespace DannyGoodacre.Testing;

/// <summary>
/// Contains fluent xUnit assertion extension methods for <see cref="ValidationState"/>.
/// </summary>
public static class ValidationStateAssertionExtensions
{
    extension(ValidationState validationState)
    {
        /// <summary>
        /// Asserts that the validation state contains no recorded errors.
        /// </summary>
        public void ShouldHaveNoErrors()
        {
            Assert.False(
                validationState.HasErrors,
                $"Expected no validation errors, but found errors for properties: {string.Join(", ", validationState.Errors.Keys)}.");
        }

        /// <summary>
        /// Asserts that a specific error message exists for the specified property name.
        /// </summary>
        public void ShouldHaveError(string propertyName, string expectedError)
        {
            Assert.True(
                validationState.Errors.TryGetValue(propertyName, out IReadOnlyList<string>? errors),
                $"Expected validation error for property '{propertyName}', but no errors were recorded for it.");

            Assert.Contains(expectedError, errors, StringComparer.Ordinal);
        }

        /// <summary>
        /// Asserts that the exact list of error messages exists for the specified property name.
        /// </summary>
        public void ShouldHaveErrorsFor(string propertyName, params string[] expectedErrors)
        {
            Assert.True(
                validationState.Errors.TryGetValue(propertyName, out IReadOnlyList<string>? errors),
                $"Expected validation errors for property '{propertyName}', but no errors were recorded for it.");

            Assert.Equal(expectedErrors, errors);
        }

        /// <summary>
        /// Performs a deep content comparison between two <see cref="ValidationState"/> instances,
        /// accounting for property key case-insensitivity and collection contents.
        /// </summary>
        public void ShouldMatch(ValidationState expected)
        {
            if (ReferenceEquals(validationState, expected))
            {
                return;
            }

            Assert.Equal(expected.HasErrors, validationState.HasErrors);

            Assert.Equal(expected.Errors.Count, validationState.Errors.Count);

            foreach (var (property, expectedErrors) in expected.Errors)
            {
                Assert.True(
                    validationState.Errors.TryGetValue(property, out IReadOnlyList<string>? actualErrors),
                    $"Expected property '{property}' in validation state, but it was missing.");

                Assert.Equal(expectedErrors, actualErrors);
            }
        }
    }
}
