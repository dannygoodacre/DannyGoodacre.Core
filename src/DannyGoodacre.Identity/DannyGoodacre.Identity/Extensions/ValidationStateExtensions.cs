using DannyGoodacre.Primitives;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DannyGoodacre.Identity;

internal static class ValidationStateExtensions
{
    extension(ValidationState state)
    {
        public ValidationProblemDetails ToValidationProblemDetails()
        {
            ModelStateDictionary modelState = new();

            foreach (KeyValuePair<string, IReadOnlyList<string>> kvp in state.Errors)
            {
                foreach (string error in kvp.Value)
                {
                    modelState.AddModelError(kvp.Key, error);
                }
            }

            return new ValidationProblemDetails(modelState);
        }
    }
}
