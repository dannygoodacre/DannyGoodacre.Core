using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity;

internal static class HttpContextExtensions
{
    extension(HttpContext httpContext)
    {
        public Guid? GetUserId()
        {
            var nameIdentifier = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(nameIdentifier, out var userId)
                ? userId
                : null;
        }

        public bool IsSelfOrAdmin(Guid id)
        {
            Guid? requestUserId = httpContext.GetUserId();

            bool isAdmin = httpContext.User.IsInRole("Admin");

            return requestUserId == id || isAdmin;
        }
    }
}
