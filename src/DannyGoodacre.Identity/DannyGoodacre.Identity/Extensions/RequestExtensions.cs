using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Models;

namespace DannyGoodacre.Identity;

internal static class RequestExtensions
{
    public static LoginUserCommand ToCommand(this LoginRequest request)
        => new()
        {
            Username = request.Username,
            Password = request.Password
        };
}
