using DannyGoodacre.Core;

namespace DannyGoodacre.Identity.Application.Extensions;

internal static class ValidationStateExtensions
{
    extension(ValidationState validationState)
    {
        public bool IsNotNullEmptyOrWhitespace(string value, string name)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            validationState.AddError(name, "Must not be null, empty, or whitespace.");

            return false;

        }
    }
}
