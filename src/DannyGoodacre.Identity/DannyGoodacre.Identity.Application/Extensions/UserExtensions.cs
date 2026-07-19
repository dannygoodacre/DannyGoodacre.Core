using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Domain.Entities;

namespace DannyGoodacre.Identity.Application;

internal static class UserExtensions
{
    extension(User)
    {
        public static User CreateNew(string username, string passwordHash, bool isApproved = false)
            => new()
            {
                PublicId = Guid.NewGuid(),
                Username = username,
                IsApproved = isApproved,
                PasswordHash = passwordHash,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
    }

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
