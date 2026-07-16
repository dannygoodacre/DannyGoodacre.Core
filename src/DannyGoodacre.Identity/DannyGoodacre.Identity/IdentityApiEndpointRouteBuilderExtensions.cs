using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DannyGoodacre.Identity;

public static class IdentityApiEndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointConventionBuilder MapIdentityEndpoints()
        {
            var group = endpoints.MapGroup("").WithTags("Identity");

            group.MapUserEndpoints();

            group.MapSessionEndpoints();

            group.MapAdminEndpoints();

            return group;
        }
    }
}
