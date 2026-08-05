using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Application.Services;

internal interface IPasswordValidatorService
{
    public Result IsPasswordValid(ValidationState state, string password);
}

internal sealed class PasswordValidatorService(IOptions<IdentityOptions> options)
    : IPasswordValidatorService
{
    private readonly PasswordValidatorOptions _options = options.Value.PasswordValidator;

    private const string Name = "Password";

    public Result IsPasswordValid(ValidationState state, string password)
    {
        if (!state.IsNotNullEmptyOrWhitespace(password, nameof(password)))
        {
            return Result.Invalid(state);
        }

        if (_options.RequireLowercase)
        {
            state.ContainsLowercase(password, Name);
        }

        if (_options.RequireUppercase)
        {
            state.ContainsUppercase(password, Name);
        }

        if (_options.RequireDigit)
        {
            state.ContainsDigit(password, Name);
        }

        if (_options.RequireNonAlphanumeric)
        {
            state.ContainsNonAlphanumeric(password, Name);
        }

        if (_options.MinimumLength > 0)
        {
            state.IsAtLeastLength(password, Name, _options.MinimumLength);
        }

        return state.HasErrors
            ? Result.Invalid(state)
            : Result.Success();
    }
}
