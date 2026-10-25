using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Identity.Responses;

namespace DannyGoodacre.Identity.Application;

internal static class ClaimExtensions
{
    extension(Claim claim)
    {
        public ClaimResponse ToResponse()
            => new(claim.PublicId, claim.Type, claim.Value);
    }

    extension(RoleClaim claim)
    {
        public ClaimResponse ToResponse()
            => new(claim.Claim.PublicId, claim.Claim.Type, claim.Claim.Value);
    }
}
