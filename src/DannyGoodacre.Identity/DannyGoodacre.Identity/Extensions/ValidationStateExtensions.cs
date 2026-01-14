using DannyGoodacre.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DannyGoodacre.Identity.Extensions;

internal static class ValidationStateExtensions
{
    extension(ValidationState validationState)
    {
        public ValidationProblemDetails ToValidationProblemDetails()
        {
            var modelState = new ModelStateDictionary();

            foreach (var kvp in validationState.Errors)
            {
                foreach (var error in kvp.Value)
                {
                    modelState.AddModelError(kvp.Key, error);
                }
            }

            return new ValidationProblemDetails(modelState);
        }
    }
}
