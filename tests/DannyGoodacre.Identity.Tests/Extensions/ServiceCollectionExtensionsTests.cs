using DannyGoodacre.Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DannyGoodacre.Identity.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    private class TestContext(DbContextOptions options) : IdentityContext(options);

    [Test]
    public void AddIdentity()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<TestContext>();

        // Act
        services.AddIdentity<TestContext>();

        var provider = services.BuildServiceProvider();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetService<IdentityContext>(), Is.Not.Null);
            Assert.That(provider.GetService<ISignInManager>(), Is.Not.Null);
            Assert.That(provider.GetService<IUserContext>(), Is.Not.Null);
            Assert.That(provider.GetService<IUserManager<Core.IdentityUser>>(), Is.Not.Null);
            Assert.That(provider.GetService<IUserStore<Core.IdentityUser>>(), Is.Not.Null);
        }
    }
}
