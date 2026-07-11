using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal class ApproveUserHandlerTests : TransactionCommandHandlerTestBase<ApproveUserHandler>
{
    protected override string CommandName => "Approve User";

    private string _requestUserId = null!;

    private IdentityUser _testUser = null!;

    private Result _testAddToRoleResult = null!;

    private Result _testUpdateAsyncResult = null!;

    private Mock<IUserManager<IdentityUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _requestUserId = "Request User Id";

        _userManagerMock = new Mock<IUserManager<IdentityUser>>(MockBehavior.Strict);

        _testUser = new IdentityUser();

        _testAddToRoleResult = Result.Success();

        _testUpdateAsyncResult = Result.Success();

        CommandHandler = new ApproveUserHandler(LoggerMock.Object,
                                         UnitOfWorkMock.Object,
                                         _userManagerMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestUserId, CancellationToken);

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task ApproveUser_WhenUserIdInvalid_ShouldReturnInvalid(string userId)
    {
        // Arrange
        _requestUserId = userId;

        LoggerMock.Setup(LogLevel.Error, $"Command '{CommandName}' failed validation: UserId:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ApproveUser_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _testUser = null!;

        SetupUserManager_FindByIdAsync();

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task ApproveUser_WhenAddToRoleFails_ShouldReturnError()
    {
        // Arrange
        const string testErrorMessage = "Test Error";

        SetupUserManager_FindByIdAsync();

        _testAddToRoleResult = Result.Failed(testErrorMessage);

        SetupUserManager_AddToRoleAsync();

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public async Task ApproveUser_WhenUpdateFails_ShouldReturnError()
    {
        // Arrange
        const string testErrorMessage = "Test Error";

        SetupUserManager_FindByIdAsync();

        SetupUserManager_AddToRoleAsync();

        _testUpdateAsyncResult = Result.Failed(testErrorMessage);

        SetupUserManager_UpdateAsync();

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public async Task ApproveUser_WhenSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        SetupUserManager_FindByIdAsync();

        SetupUserManager_AddToRoleAsync();

        SetupUserManager_UpdateAsync();

        SetupTransaction_SaveChangesAndCommitAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupUserManager_FindByIdAsync()
        => _userManagerMock
            .Setup(x => x.FindByIdAsync(
                It.Is<string>(y => y == _requestUserId)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);

    private void SetupUserManager_AddToRoleAsync()
        => _userManagerMock
            .Setup(x => x.AddToRoleAsync(
                It.Is<IdentityUser>(y => y == _testUser),
                It.Is<string>(y => y == "User")))
            .ReturnsAsync(_testAddToRoleResult)
            .Verifiable(Times.Once);

    private void SetupUserManager_UpdateAsync()
        => _userManagerMock
            .Setup(x => x.UpdateAsync(
                It.Is<IdentityUser>(y => y == _testUser)))
            .ReturnsAsync(_testUpdateAsyncResult)
            .Verifiable(Times.Once);
}
