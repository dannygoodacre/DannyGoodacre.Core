using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Commands;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal class LogoutTests : CommandHandlerTestBase<LogoutHandler>
{
    protected override string CommandName => "Logout";

    private Mock<ISignInManager> _signInManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _signInManagerMock = new Mock<ISignInManager>(MockBehavior.Strict);

        CommandHandler = new LogoutHandler(LoggerMock.Object, _signInManagerMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(CancellationToken);

    [Test]
    public async Task Logout_WhenSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        SetupSignInManager_SignOutAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupSignInManager_SignOutAsync()
        => _signInManagerMock
            .Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);
}
