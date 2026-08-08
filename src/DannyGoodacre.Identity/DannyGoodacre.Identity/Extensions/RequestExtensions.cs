using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Entities;
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

    public static AddUserCommand ToCommand(this RegistrationRequest request)
        => new()
        {
            Username = request.Username,
            Password = request.Password
        };

    public static AddRoleCommand ToCommand(this AddRoleRequest request)
        => new()
        {
            Name = request.Name,
            ClaimIds = request.ClaimIds
        };
}
