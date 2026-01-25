using DannyGoodacre.Identity.Application.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Tests.Extensions;

[TestFixture]
public class IdentityApiEndpointRouteBuilderExtensionsTests : TestBase
{
    [Test]
    public void Foo()
    {
        // Arrange
        var services = new ServiceCollection();

        var registerNewUserMock = new Mock<IRegisterNewUser>();

        services.AddSingleton(registerNewUserMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var routeBuilderMock = new Mock<IEndpointRouteBuilder>();

        var dataSources = new List<EndpointDataSource>();

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
            Assert.That(endpoints.Any(x => x.RoutePattern.RawText == "/auth/register"));

            var registerEndpoint = dataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>().FirstOrDefault(x => x.RoutePattern.RawText == "/auth/register");

            var methodMetadata = registerEndpoint?.Metadata.GetMetadata<IHttpMethodMetadata>();

            Assert.That(methodMetadata?.HttpMethods, Contains.Item("POST"));
            Assert.That(methodMetadata?.HttpMethods.Count, Is.EqualTo(1));
        }
    }

    private void AssertEndpoint(string endpoint, string method)
    {

    }
}
