using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application.Extensions;

internal static class UserExtensions
{
    extension(User user)
    {
        public UserInfo ToUserInfoResponse()
            => new()
            {
                Id = user.PublicId,
                Username = user.Username,
                IsApproved = user.IsApproved
            };
    }
}
