using DannyGoodacre.Core;

namespace DannyGoodacre.Identity.Application.Abstractions;

public interface ISignInManager
{
    Task<Result> PasswordSignInAsync(string username, string password);

    Task SignOutAsync();
}
