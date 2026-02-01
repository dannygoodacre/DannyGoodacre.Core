using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Extensions;
using DannyGoodacre.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Application.Services;

internal interface IPasswordValidatorService
{
    public Result IsPasswordValid(ValidationState state, string password);
}

internal sealed class PasswordValidatorService(IOptions<PasswordValidatorOptions> options)
    : IPasswordValidatorService
{
    private readonly PasswordValidatorOptions _options = options.Value;

    private const string Name = "Password";

    public Result IsPasswordValid(ValidationState state, string password)
    {
        state
            .If(_options.RequiresLowercase,
                x => x.DoesContainLowercase(password, Name))
            .If(_options.RequiresUppercase,
                x => x.DoesContainUppercase(password, Name))
            .If(_options.RequireDigit,
                x => x.DoesContainDigit(password, Name))
            .If(_options.RequiresNonAlphanumeric,
                x => x.DoesContainNonAlphanumeric(password, Name))
            .If(_options.MinimumLength > 0,
                x => x.IsAtLeastMinimumLength(password, Name, _options.MinimumLength));

        return state.HasErrors
            ? Result.Invalid(state)
            : Result.Success();
    }
}
