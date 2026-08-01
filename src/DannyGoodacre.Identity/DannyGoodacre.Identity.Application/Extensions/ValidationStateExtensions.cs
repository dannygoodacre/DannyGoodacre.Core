using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application;

internal static class ValidationStateExtensions
{
    extension(ValidationState state)
    {
        public bool IsNotNullEmptyOrWhitespace(string value, string name)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            state.AddError(name, "Must not be null, empty, or whitespace.");

            return false;

        }

        public bool IsAtLeastLength(string value, string name, int minLength)
        {
            if (value.Length >= minLength)
            {
                return true;
            }

            state.AddError(name, $"Must be at least {minLength} characters long.");

            return false;

        }

        public bool IsNonEmptyGuid(Guid value, string name)
        {
            if (value != Guid.Empty)
            {
                return true;
            }

            state.AddError(name, "Must not be empty.");

            return false;
        }

        public bool ContainsNonAlphanumeric(string value, string name)
        {
            if (!value.All(x => char.IsUpper(x) || char.IsLower(x) || char.IsDigit(x)))
            {
                return true;
            }

            state.AddError(name, "Must contain at least one non-alphanumeric character.");

            return false;

        }

        public bool ContainsLowercase(string value, string name)
        {
            if (value.Any(char.IsLower))
            {
                return true;
            }

            state.AddError(name, "Must contain at least one lowercase character.");

            return false;

        }

        public bool ContainsUppercase(string value, string name)
        {
            if (value.Any(char.IsUpper))
            {
                return true;
            }

            state.AddError(name, "Must contain at least one uppercase character.");

            return false;

        }

        public bool ContainsDigit(string value, string name)
        {
            if (value.Any(char.IsDigit))
            {
                return true;
            }

            state.AddError(name, "Must contain at least one digit.");

            return false;

        }
    }
}
