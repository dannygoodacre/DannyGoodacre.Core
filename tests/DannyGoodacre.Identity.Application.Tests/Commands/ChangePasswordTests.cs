using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class ChangePasswordTests : CommandHandlerTestBase<ChangePasswordHandler>
{
    protected override string CommandName => "Change Password";

    private string _requestCurrentPassword = null!;

    private string _requestNewPassword = null!;

    private string? _testUserId;

    private IdentityUser? _testUser;

    private Result _testChangePasswordResult = null!;

    private Mock<IUserContext> _userContextMock = null!;

    private Mock<IUserManager<IdentityUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _requestCurrentPassword = "Request Current Password";

        _requestNewPassword = "Request New Password";

        _testUserId = "Test User Id";

        _testUser = new IdentityUser();

        _testChangePasswordResult = Result.Success();

        _userContextMock = new Mock<IUserContext>(MockBehavior.Strict);

        _userManagerMock = new Mock<IUserManager<IdentityUser>>(MockBehavior.Strict);

        CommandHandler = new ChangePasswordHandler(LoggerMock.Object, _userContextMock.Object, _userManagerMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestCurrentPassword, _requestNewPassword, CancellationToken);

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task ChangePassword_WhenPasswordInvalid_ShouldReturnInvalid(string currentPassword)
    {
        // Arrange
        _requestCurrentPassword = currentPassword;

        SetupLogger_FailedValidation($"CurrentPassword:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ChangePassword_WhenUserIdNull_ShouldReturnDomainError()
    {
        // Arrange
        _testUserId = null;

        SetupUserContext_GetUserId();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, "User not found");
    }

    [Test]
    public async Task ChangePassword_WhenUserIsNull_ShouldReturnDomainError()
    {
        // Arrange
        _testUser = null;

        SetupUserContext_GetUserId();

        SetupUserManager_FindByIdAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, "User not found");
    }

    [Test]
    public async Task ChangePassword_WhenChangePasswordFails_ShouldReturnError()
    {
        // Arrange
        const string testErrorMessage = "Error Message";

        _testChangePasswordResult = Result.DomainError(testErrorMessage);

        SetupUserContext_GetUserId();

        SetupUserManager_FindByIdAsync();

        SetupUserManager_ChangePasswordAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, "Error Message");
    }

    [Test]
    public async Task ChangePassword_WhenSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        SetupUserContext_GetUserId();

        SetupUserManager_FindByIdAsync();

        SetupUserManager_ChangePasswordAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupUserContext_GetUserId()
        => _userContextMock
            .Setup(x => x.GetUserId())
            .Returns(_testUserId)
            .Verifiable(Times.Once);

    private void SetupUserManager_FindByIdAsync()
        => _userManagerMock
            .Setup(x => x.FindByIdAsync(
                It.Is<string>(y => y == _testUserId)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);

    private void SetupUserManager_ChangePasswordAsync()
        => _userManagerMock
            .Setup(x => x.ChangePasswordAsync(
                It.Is<IdentityUser>(y => y == _testUser),
                It.Is<string>(y => y == _requestCurrentPassword),
                It.Is<string>(y => y == _requestNewPassword)))
            .ReturnsAsync(_testChangePasswordResult)
            .Verifiable(Times.Once);
}
