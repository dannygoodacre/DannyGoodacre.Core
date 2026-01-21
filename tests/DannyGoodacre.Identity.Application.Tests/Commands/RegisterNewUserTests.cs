using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal class RegisterNewUserTests : TransactionCommandHandlerTestBase<RegisterNewUserHandler>
{

    protected override string CommandName => "Register New User";

    private string _requestUsername = null!;

    private string _requestPassword = null!;

    private IdentityUser _testUser = null!;

    private Result _testCreateResult = null!;

    private Mock<IUserStore<IdentityUser>> _userStoreMock = null!;

    private Mock<IUserManager<IdentityUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _testCreateResult = Result.Success();

        _userStoreMock = new Mock<IUserStore<IdentityUser>>(MockBehavior.Strict);

        _userManagerMock = new Mock<IUserManager<IdentityUser>>(MockBehavior.Strict);

        CommandHandler = new RegisterNewUserHandler(LoggerMock.Object,
                                                    UnitOfWorkMock.Object,
                                                    _userStoreMock.Object,
                                                    _userManagerMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestUsername, _requestPassword, CancellationToken);

    [TestCase(null!)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task RegisterNewUser_WhenUsernameInvalid_ShouldReturnInvalid(string username)
    {
        // Arrange
        _requestUsername = username;

        SetupLogger_FailedValidation($"Username:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task RegisterNewUser_WhenCreateUserFails_ShouldReturnError()
    {
        // Arrange
        const string testErrorMessage = "Test Error";

        _testCreateResult = Result.DomainError(testErrorMessage);

        SetupUserStore_SetUsernameAsync();

        SetupUserManager_CreateAsync();

        SetupTransaction_RollbackAsync();

        // Act
        var result = await Act();

        // Assert
        AssertDomainError(result, testErrorMessage);
    }

    [Test]
    public async Task RegisterNewUser_WhenSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        SetupUserStore_SetUsernameAsync();

        SetupUserManager_CreateAsync();

        SetupTransaction_SaveChangesAndCommitAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupUserStore_SetUsernameAsync()
        => _userStoreMock
            .Setup(x => x.SetUsernameAsync(
                It.IsAny<IdentityUser>(),
                It.Is<string>(y => y == _requestUsername),
                It.Is<CancellationToken>(y => y == CancellationToken)))
            .Callback<IdentityUser, string, CancellationToken>(
                (user, _, _) => _testUser = user)
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

    private void SetupUserManager_CreateAsync()
        => _userManagerMock
            .Setup(x => x.CreateAsync(
                It.Is<IdentityUser>(y => y == _testUser),
                It.Is<string>(y => y == _requestPassword)))
            .ReturnsAsync(_testCreateResult)
            .Verifiable(Times.Once);
}
