using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DannyGoodacre.Core.Identity;

internal static class ValidationStateExtensions
{
    public static ValidationProblemDetails ToValidationProblemDetails(this ValidationState validationState)
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
