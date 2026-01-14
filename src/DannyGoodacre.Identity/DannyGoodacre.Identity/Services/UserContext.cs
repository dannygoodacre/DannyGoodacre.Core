using System.Security.Claims;
using DannyGoodacre.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity.Services;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string? GetUserId()
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity is null || !httpContext.User.Identity.IsAuthenticated)
        {
            return null;
        }

        return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
