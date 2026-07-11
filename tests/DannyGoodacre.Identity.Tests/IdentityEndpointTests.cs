using System.Net;
using DannyGoodacre.Identity.Models;
using DannyGoodacre.Identity.Tests.Harness;
using Microsoft.AspNetCore.Mvc;

namespace DannyGoodacre.Identity.Tests;

[TestFixture]
internal sealed class IdentityEndpointTests
{
    private IdentityWebApplicationFactory _factory;

    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new IdentityWebApplicationFactory();

        _client = _factory.CreateClient();
    }

    [Test]
    public async Task Post_Users_WhenInvalidUsername_ShouldReturn()
    {
        // Arrange
        const string username = " ";
        const string password = "TestPassword123$";

        var registrationRequest = new RegistrationRequest
        {
            Username = username,
            Password = password,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users", registrationRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem?.Errors, Does.ContainKey("Username"));

            Assert.That(problem?.Errors["Username"], Does.Contain("Must not be null, empty, or whitespace."));
        }
    }

    [Test]
    public async Task Post_Users()
    {
        // Arrange
        const string username = "TestUser";

        const string password = "abc";

        var registrationRequest = new RegistrationRequest
        {
            Username = username,
            Password = password,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users", registrationRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem?.Errors, Does.ContainKey("Username"));

            Assert.That(problem?.Errors["Username"], Does.Contain("Must not be null, empty, or whitespace."));
        }
    }
}
