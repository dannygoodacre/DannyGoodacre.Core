using DannyGoodacre.Identity.Domain.Entities;
using DannyGoodacre.Identity.Responses;

namespace DannyGoodacre.Identity;

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
        public UserInfoResponse ToUserInfoResponse()
            => new(user.PublicId, user.Username, user.IsApproved);
    }
}
