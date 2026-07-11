using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal class LoginTests : CommandHandlerTestBase<LoginHandler>
{
    protected override string CommandName => "Login";

    private string _requestUsername = null!;

    private string _requestPassword = null!;

    private IdentityUser _testUser = null!;

    private bool _testIsUserConfirmed;

    private Result _testPasswordSignInResult = null!;

    private Mock<IUserManager<IdentityUser>> _userManagerMock = null!;

    private Mock<ISignInManager> _signInManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _testUser = new IdentityUser();

        _testIsUserConfirmed = true;

        _testPasswordSignInResult = Result.Success();

        _userManagerMock = new Mock<IUserManager<IdentityUser>>(MockBehavior.Strict);

        _signInManagerMock = new Mock<ISignInManager>(MockBehavior.Strict);

        CommandHandler = new LoginHandler(LoggerMock.Object,
                                          _userManagerMock.Object,
                                          _signInManagerMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestUsername, _requestPassword, CancellationToken);

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task Login_WhenUsernameInvalid_ShouldReturnInvalid(string username)
    {
        // Arrange
        _requestUsername = username;

        SetupLogger_FailedValidation($"Username:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task Login_WhenUserNotFound_ShouldReturnDomainError()
    {
        // Arrange
        _testUser = null!;

        SetupUserManager_FindByUsernameAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, "User not found.");
    }

    [Test]
    public async Task Login_WhenUserNotConfirmed_ShouldReturnDomainError()
    {
        // Arrange
        SetupUserManager_FindByUsernameAsync();

        _testIsUserConfirmed = false;

        SetupUserManager_IsUserConfirmedAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, "User not confirmed.");
    }

    [Test]
    public async Task Login_WhenSignInFails_ShouldReturnError()
    {
        // Arrange
        const string testErrorMessage = "Test Error";

        SetupUserManager_FindByUsernameAsync();

        SetupUserManager_IsUserConfirmedAsync();

        _testPasswordSignInResult = Result.Failed(testErrorMessage);

        SetupSignInManager_PasswordSignInAsync();

        // Act
        var  result = await Act();

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public async Task Login_WhenSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        SetupUserManager_FindByUsernameAsync();

        SetupUserManager_IsUserConfirmedAsync();

        SetupSignInManager_PasswordSignInAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupUserManager_FindByUsernameAsync()
        => _userManagerMock
            .Setup(x => x.FindByUsernameAsync(
                It.Is<string>(y => y == _requestUsername)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);

    private void SetupUserManager_IsUserConfirmedAsync()
        => _userManagerMock
            .Setup(x => x.IsUserConfirmedAsync(
                It.Is<IdentityUser>(y => y == _testUser)))
            .ReturnsAsync(_testIsUserConfirmed)
            .Verifiable(Times.Once);

    private void SetupSignInManager_PasswordSignInAsync()
        => _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(
                It.Is<string>(y => y == _requestUsername),
                It.Is<string>(y => y == _requestPassword)))
            .ReturnsAsync(_testPasswordSignInResult)
            .Verifiable(Times.Once);
}
