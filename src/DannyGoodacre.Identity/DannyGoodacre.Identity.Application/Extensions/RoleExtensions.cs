using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Extensions;

internal static class RoleExtensions
{
    extension(Role role)
    {
        public RoleResponse ToResponse()
            => new()
            {
                Id = role.PublicId,
                Name = role.Name,
                Claims = role.Claims.Select(x => x.ToResponse()).ToList()
            };
    }
}
