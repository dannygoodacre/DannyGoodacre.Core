using DannyGoodacre.Core;

namespace DannyGoodacre.Identity.Application.Extensions;

internal static class ValidationStateExtensions
{
    extension(ValidationState state)
    {
        public ValidationState If(bool condition, Action<ValidationState> action)
        {
            if (condition)
            {
                action(state);
            }

            return state;
        }

        public ValidationState IsNotNullEmptyOrWhitespace(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                state.AddError(name, "Must not be null, empty, or whitespace.");
            }

            return state;
        }

        public ValidationState IsAtLeastMinimumLength(string value, string name, int minLength)
        {
            if (value.Length < minLength)
            {
                state.AddError(name, $"Must be at least {minLength} characters long.");
            }

            return state;
        }

        public ValidationState DoesContainNonAlphanumeric(string value, string name)
        {
            if (value.All(x => char.IsUpper(x) || char.IsLower(x) || char.IsDigit(x)))
            {
                state.AddError(name, "Must contain at least one non-alphanumeric character.");
            }

            return state;
        }

        public ValidationState DoesContainLowercase(string value, string name)
        {
            if (!value.Any(char.IsLower))
            {
                state.AddError(name, "Must contain at least one lowercase character.");
            }

            return state;
        }

        public ValidationState DoesContainUppercase(string value, string name)
        {
            if (!value.Any(char.IsUpper))
            {
                state.AddError(name, "Must contain at least one uppercase character.");
            }

            return state;
        }

        public ValidationState DoesContainDigit(string value, string name)
        {
            if (!value.Any(char.IsDigit))
            {
                state.AddError(name, "Must contain at least one digit.");
            }

            return state;
        }
    }
}
