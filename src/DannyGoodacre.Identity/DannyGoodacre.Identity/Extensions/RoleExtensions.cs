using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Identity.Responses;

namespace DannyGoodacre.Identity.Application;

internal static class RoleExtensions
{
    extension(Role role)
    {
        public RoleResponse ToResponse() => new(role.PublicId, role.Name, role.Claims.Select(x => x.ToResponse()).ToList(), role.IsDisabled);
    }
}
