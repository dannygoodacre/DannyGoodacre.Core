using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Extensions;

public static class UserExtensions
{
    extension(User user)
    {
        public UserInfo ToUserInfoResponse()
            => new()
            {
                Username = user.Username,
                IsApproved = user.IsApproved
            };
    }
}
