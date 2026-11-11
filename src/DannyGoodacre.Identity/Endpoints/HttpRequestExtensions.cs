using Microsoft.AspNetCore.Http;

namespace DannyGoodacre.Identity.Endpoints;

internal static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        /// <summary>
        /// Builds a <c>Location</c> from the request path rather than a route name, so endpoint groups mapped under
        /// different prefixes do not depend on each other.
        /// </summary>
        public string LocationOf(Guid id)
            => $"{request.PathBase}{request.Path.Value?.TrimEnd('/')}/{id}";
    }
}
