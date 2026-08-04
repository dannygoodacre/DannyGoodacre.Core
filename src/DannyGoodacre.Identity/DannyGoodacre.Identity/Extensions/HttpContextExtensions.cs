using System.Security.Claims;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity;

internal static class HttpContextExtensions
{
    extension(HttpContext httpContext)
    {
        public bool IsSelfOrAdmin(Guid id)
        {
            Guid? requestUserId = httpContext.UserId;

            bool isAdmin = httpContext.User.IsInRole("Admin");

            return requestUserId == id || isAdmin;
        }

        public SessionInfoResponse SessionInfoResponse
            => new()
            {
                UserId = httpContext.UserId,
                Username = httpContext.User.Identity?.Name,
                IsAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false,
                Claims = httpContext.User.Claims.Select(x => new ClaimDefinition
                {
                    Type = x.Type,
                    Value = x.Value
                }).ToList(),
                Roles = httpContext.User
                    .FindAll(ClaimTypes.Role)
                    .Select(x => x.Value)
                    .ToList()
            };

        public Guid? UserId
            => Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId)
                ? userId
                : null;
    }
}
