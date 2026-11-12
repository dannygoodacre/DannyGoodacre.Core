using System.Security.Claims;

namespace DannyGoodacre.Identity;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal principal)
    {
        /// <summary>
        /// Reads the signed-in user's <see cref="User"/> id from the <see cref="ClaimTypes.NameIdentifier"/> claim.
        /// </summary>
        /// <returns>The id, or <see langword="null"/> when the principal is not signed in.</returns>
        public Guid? GetUserId()
            => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) 
                ? id 
                : null;
    }
}
