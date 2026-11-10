using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Primitives;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Application.Services;

internal interface IPasswordValidatorService
{
    IResult IsPasswordValid(ValidationState state, string password);
}

internal sealed class PasswordValidatorService(IOptions<IdentityOptions> options)
    : IPasswordValidatorService
{
    private readonly PasswordValidatorOptions _options = options.Value.PasswordValidator;

    private const string Name = "Password";

    public IResult IsPasswordValid(ValidationState state, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            state.AddError(Name, "Password is required.");
            return Result.Invalid(state);
        }

        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            state.AddError(Name, "Password must contain at least one lowercase letter ('a'-'z').");
        }

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            state.AddError(Name, "Password must contain at least one uppercase letter ('A'-'Z').");
        }

        if (_options.RequireDigit && !password.Any(char.IsDigit))
        {
            state.AddError(Name, "Password must contain at least one digit ('0'-'9').");
        }

        if (_options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            state.AddError(Name, "Password must contain at least one non-alphanumeric character.");
        }

        if (_options.MinimumLength > 0 && password.Length < _options.MinimumLength)
        {
            state.AddError(Name, $"Password must be at least {_options.MinimumLength} characters long.");
        }

        return state.HasErrors
            ? Result.Invalid(state)
            : Result.Success();
    }
}
