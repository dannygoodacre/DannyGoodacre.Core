using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Entities;

namespace DannyGoodacre.Identity.Application;

internal static class ClaimExtensions
{
    extension(Claim claim)
    {
        public ClaimResponse ToResponse()
            => new()
            {
                Id = claim.PublicId,
                Type = claim.Type,
                Value = claim.Value
            };
    }

    extension(RoleClaim claim)
    {
        public ClaimResponse ToResponse()
            => new()
            {
                Id = claim.Claim.PublicId,
                Type = claim.Claim.Type,
                Value = claim.Claim.Value
            };
    }
}
