using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Extensions;

internal static class ClaimExtensions
{
    extension(Claim value)
    {
        public ClaimResponse ToResponse()
            => new()
            {
                Id = value.PublicId,
                Type = value.Type,
                Value = value.Value
            };
    }
}
