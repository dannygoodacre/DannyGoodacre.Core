using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class LoginUserTests : StateCommandHandlerTestBase<LoginUserHandler, Guid>
{
    protected override string CommandName => "Login User";

    private string _requestUsername = null!;

    private string _requestPassword = null!;

    private LoginUserCommand _requestCommand = null!;

    private User? _testUser;

    private bool _testVerifyPasswordResult;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    private Mock<IPasswordHashingService> _hashingServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        _hashingServiceMock = new Mock<IPasswordHashingService>(MockBehavior.Strict);

        CommandHandler = new LoginUserHandler(
            LoggerMock.Object,
            StateUnitMock.Object,
            _userRepositoryMock.Object,
            _hashingServiceMock.Object);
    }

    protected override Task<Result<Guid>> Act()
        => CommandHandler.ExecuteAsync(_requestCommand, TestCancellationToken);

    [Test]
    public async Task WhenRequestInvalid_ShouldReturnInvalid()
    {
        // Arrange
        _requestCommand = new LoginUserCommand
        {
            Username = string.Empty,
            Password = null!
        };

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Username:{Environment.NewLine}  - Must not be null, empty, or whitespace.{Environment.NewLine}Password:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        // Act
        Result<Guid> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenUserIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new LoginUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = null;

        SetupUserRepository_GetWithTrackingAsync();

        SetupLogger_LogNotFound();

        // Act
        Result<Guid> result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenUserNotApproved_ShouldReturnDomainError()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new LoginUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = _requestUsername,
            IsApproved = false,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_GetWithTrackingAsync();

        SetupLogger_LogUserNotApproved();

        // Act
        Result<Guid> result = await Act();

        // Assert
        AssertDomainError(result, "User not approved");
    }

    [Test]
    public async Task WhenPasswordIncorrect_ShouldReturnDomainError()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new LoginUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = _requestUsername,
            IsApproved = true,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_GetWithTrackingAsync();

        _testVerifyPasswordResult = false;

        SetupHashingService_Verify();

        SetupLogger_LogIncorrectPassword();

        // Act
        Result<Guid> result = await Act();

        // Assert
        AssertDomainError(result, "Incorrect password");
    }

    [Test]
    public async Task WhenValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new LoginUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = _requestUsername,
            IsApproved = true,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        _testVerifyPasswordResult = true;

        SetupUserRepository_GetWithTrackingAsync();

        SetupHashingService_Verify();

        SetupLogger_LogCompleted();

        SetupStateUnit_SaveChangesAsync();

        // Act
        Result<Guid> result = await Act();

        // Assert
        AssertSuccess(result);
        Assert.That(result.Value, Is.EqualTo(_testUser.PublicId));
    }

    private void SetupLogger_LogStarted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' started for Username '{_requestUsername}'.");

    private void SetupLogger_LogNotFound()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed: User with Username '{_requestUsername}' not found.");

    private void SetupLogger_LogUserNotApproved()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed: User with Username '{_requestUsername}' not approved.");

    private void SetupLogger_LogIncorrectPassword()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed: Incorrect password provided for Username '{_requestUsername}'.");

    private void SetupLogger_LogCompleted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' completed for Username '{_requestUsername}'.");

    private void SetupUserRepository_GetWithTrackingAsync()
        => _userRepositoryMock
            .Setup(x => x.GetWithTrackingAsync(
                It.Is<string>(y => y == _requestUsername),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);

    private void SetupHashingService_Verify()
        => _hashingServiceMock
            .Setup(x => x.Verify(
                It.Is<string>(y => y == _requestPassword),
                It.Is<string>(y => y == _testUser!.PasswordHash)))
            .Returns(_testVerifyPasswordResult)
            .Verifiable(Times.Once);
}
