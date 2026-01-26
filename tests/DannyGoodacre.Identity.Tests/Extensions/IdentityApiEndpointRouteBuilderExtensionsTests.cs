using DannyGoodacre.Identity.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Tests.Extensions;

[TestFixture]
public class IdentityApiEndpointRouteBuilderExtensionsTests : TestBase
{
    [Test]
    public void MapIdentityEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();

        var registerNewUserMock = new Mock<IRegisterNewUser>();

        services.AddSingleton(registerNewUserMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var dataSources = new List<EndpointDataSource>();

        var routeBuilderMock = new Mock<IEndpointRouteBuilder>();

        routeBuilderMock
            .Setup(x => x.DataSources)
            .Returns(dataSources);

        routeBuilderMock
            .Setup(x => x.CreateApplicationBuilder())
            .Returns(new ApplicationBuilder(serviceProvider));

        // Act
        routeBuilderMock.Object.MapIdentityEndpoints();

        // Assert
        var endpoints = dataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>().ToList();

        using (Assert.EnterMultipleScope())
        {
            var register = GetEndpoint(endpoints, "/users", "POST");
            Assert.That(register, Is.Not.Null);

            var getUser = GetEndpoint(endpoints, "/users/{userId}", "GET");
            Assert.That(getUser, Is.Not.Null);

            var login = GetEndpoint(endpoints, "/session", "POST");
            Assert.That(login, Is.Not.Null);

            var logout = GetEndpoint(endpoints, "/session", "DELETE");
            Assert.That(logout, Is.Not.Null);

            var authMetadata = logout?.Metadata.GetMetadata<IAuthorizeData>();
            Assert.That(authMetadata, Is.Not.Null);
        }
    }

    private static RouteEndpoint? GetEndpoint(IEnumerable<RouteEndpoint> endpoints, string pattern, string method)
        => endpoints.FirstOrDefault(e =>
            e.RoutePattern.RawText == pattern
            && e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(method) == true);
}
