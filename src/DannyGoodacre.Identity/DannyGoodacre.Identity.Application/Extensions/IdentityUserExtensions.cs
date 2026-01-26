using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Extensions;

internal static class IdentityUserExtensions
{
    extension(IdentityUser user)
    {
        public UserInfoResponse ToUserInfoResponse()
            => new UserInfoResponse
            {
                Id = user.Id,
                Username = user.UserName!,
                IsApproved = user.EmailConfirmed,
            };
    }
}
