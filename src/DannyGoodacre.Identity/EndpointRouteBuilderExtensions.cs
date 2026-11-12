using DannyGoodacre.Identity.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity;

public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>POST /sessions</c> (sign in), <c>DELETE /sessions</c> (sign out), and <c>POST /password-reset</c>.
        /// </summary>
        public RouteGroupBuilder MapIdentitySessionEndpoints()
            => endpoints.MapIdentityGroup(SessionEndpoints.Map);

        /// <summary>
        /// Maps <c>POST /users</c> (registration), <c>GET /users/me</c>, <c>PUT /users/me/password</c>, and
        /// <c>DELETE /users/{id}</c>.
        /// </summary>
        public RouteGroupBuilder MapIdentityUserEndpoints()
            => endpoints.MapIdentityGroup(UserEndpoints.Map);

        /// <summary>
        /// Maps the admin-only endpoints: the user list, enabling and disabling accounts, password reset tokens, admin
        /// membership, and the invite code.
        /// </summary>
        public RouteGroupBuilder MapIdentityAdminEndpoints()
            => endpoints.MapIdentityGroup(AdminEndpoints.Map);

        /// <summary>
        /// Maps the session, user, and admin endpoints under one parent group.
        /// </summary>
        public RouteGroupBuilder MapIdentityEndpoints()
        {
            var group = endpoints.MapGroup("");

            group.MapIdentitySessionEndpoints();
            group.MapIdentityUserEndpoints();
            group.MapIdentityAdminEndpoints();

            return group;
        }

        private RouteGroupBuilder MapIdentityGroup(Action<RouteGroupBuilder> map)
        {
            var group = endpoints.MapGroup("");

            map(group);

            return group;
        }
    }
}
