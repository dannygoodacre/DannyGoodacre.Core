using DannyGoodacre.Results;
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
        /// Asserts that the specified property has an error with the given code, among any others.
        /// </summary>
        public void ShouldHaveErrorCode(string propertyName, string expectedCode)
        {
            IReadOnlyList<ValidationError> errors = GetErrors(validationState, propertyName);

            Assert.True(
                errors.Any(e => e.Code == expectedCode),
                $"Expected an error with code '{expectedCode}' for property '{propertyName}', but found codes: {FormatCodes(errors)}.");
        }

        /// <summary>
        /// Asserts that the specified property has an error equal to <paramref name="expected"/>, among any others.
        /// Arguments are compared by content.
        /// </summary>
        public void ShouldHaveError(string propertyName, ValidationError expected)
        {
            IReadOnlyList<ValidationError> errors = GetErrors(validationState, propertyName);

            Assert.True(
                errors.Contains(expected),
                $"Expected error '{expected.Code}' ({expected.Message}) for property '{propertyName}', but found codes: {FormatCodes(errors)}.");
        }

        /// <summary>
        /// Asserts that the specified property has exactly these error codes, in this order.
        /// </summary>
        public void ShouldHaveErrorCodesFor(string propertyName, params string[] expectedCodes)
        {
            IReadOnlyList<ValidationError> errors = GetErrors(validationState, propertyName);

            Assert.True(
                errors.Select(e => e.Code).SequenceEqual(expectedCodes),
                $"Expected codes [{string.Join(", ", expectedCodes)}] for property '{propertyName}', but found codes: {FormatCodes(errors)}.");
        }

        /// <summary>
        /// Asserts that the specified property has exactly these errors, in this order.
        /// Arguments are compared by content.
        /// </summary>
        public void ShouldHaveErrorsFor(string propertyName, params ValidationError[] expected)
        {
            IReadOnlyList<ValidationError> errors = GetErrors(validationState, propertyName);

            Assert.True(
                errors.SequenceEqual(expected),
                $"Expected errors [{string.Join(", ", expected.Select(e => e.Code))}] for property '{propertyName}', but found codes: {FormatCodes(errors)}.");
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
                    validationState.Errors.TryGetValue(property, out IReadOnlyList<ValidationError>? actualErrors),
                    $"Expected property '{property}' in validation state, but it was missing.");

                Assert.True(
                    expectedErrors.SequenceEqual(actualErrors),
                    $"Expected codes [{string.Join(", ", expectedErrors.Select(e => e.Code))}] for property '{property}', but found codes: {FormatCodes(actualErrors)}.");
            }
        }
    }

    private static IReadOnlyList<ValidationError> GetErrors(ValidationState validationState, string propertyName)
    {
        Assert.True(
            validationState.Errors.TryGetValue(propertyName, out IReadOnlyList<ValidationError>? errors),
            $"Expected validation errors for property '{propertyName}', but no errors were recorded for it.");

        return errors;
    }

    private static string FormatCodes(IEnumerable<ValidationError> errors)
        => $"[{string.Join(", ", errors.Select(e => e.Code))}]";
}
