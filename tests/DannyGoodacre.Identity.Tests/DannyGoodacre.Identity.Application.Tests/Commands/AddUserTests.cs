using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Identity.Hashing;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class AddUserTests : StateCommandHandlerTestBase<AddUserHandler, UserInfoResponse>
{
    protected override string CommandName => "Add User";

    private string _requestUsername = null!;

    private string _requestPassword = null!;

    private AddUserCommand _requestCommand = null!;

    private bool _testUserExistsResult;

    private string _testPasswordHash = null!;

    private User _testAddedUser = null!;

    private Mock<IPasswordValidatorService> _passwordValidatorServiceMock = null!;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    private Mock<IPasswordHashingService> _hashingServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _passwordValidatorServiceMock = new Mock<IPasswordValidatorService>(MockBehavior.Strict);

        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        _hashingServiceMock = new Mock<IPasswordHashingService>(MockBehavior.Strict);

        CommandHandler = new AddUserHandler(
            LoggerMock.Object,
            StateUnitMock.Object,
            _passwordValidatorServiceMock.Object,
            _userRepositoryMock.Object,
            _hashingServiceMock.Object);
    }

    protected override Task<Result<UserInfoResponse>> Act()
        => CommandHandler.ExecuteAsync(_requestCommand, TestCancellationToken);

    [Test]
    public async Task WhenPasswordInvalid_ShouldReturnInvalid()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new AddUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupPasswordValidatorService_IsPasswordValid(isValid: false);

        SetupLogger_FailedValidation($"Password:{Environment.NewLine}  - Must be valid");

        // Act
        Result<UserInfoResponse> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenUsernameAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new AddUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        SetupPasswordValidatorService_IsPasswordValid(isValid: true);

        _testUserExistsResult = true;

        SetupUserRepository_ExistsAsync();

        // Act
        Result<UserInfoResponse> result = await Act();

        // Assert
        AssertConflict(result, "Username already taken");
    }

    [Test]
    public async Task WhenUserIsValid_ShouldReturnSuccess()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _requestCommand = new AddUserCommand
        {
            Username = _requestUsername,
            Password = _requestPassword
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        SetupPasswordValidatorService_IsPasswordValid(isValid: true);

        _testUserExistsResult = false;

        SetupUserRepository_ExistsAsync();

        _testPasswordHash = "Test Password Hash";

        SetupHashingService_Hash();

        _testAddedUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = "Test Username",
            IsApproved = false,
            PasswordHash = _testPasswordHash,
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_Add();

        SetupLogger_LogCompleted();

        SetupStateUnit_SaveChangesAsync();

        var expectedUserInfoResponse = new UserInfoResponse
        {
            Id = _testAddedUser.PublicId,
            Username = _testAddedUser.Username,
            IsApproved = _testAddedUser.IsApproved,
        };

        // Act
        Result<UserInfoResponse> result = await Act();

        // Assert
        AssertSuccess(result, expectedUserInfoResponse);
    }

    private void SetupPasswordValidatorService_IsPasswordValid(bool isValid)
        => _passwordValidatorServiceMock
            .Setup(x => x.IsPasswordValid(
                It.IsAny<ValidationState>(),
                It.Is<string>(y => y == _requestPassword)))
            .Callback<ValidationState, string>((state, _) =>
            {
                if (!isValid)
                {
                    state.AddError("Password", "Must be valid");
                }
            })
            .Returns(isValid ? Result.Success() : Result.Invalid(null!))
            .Verifiable(Times.Once);

    private void SetupLogger_LogStarted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' started for Username '{_requestUsername}'.");

    private void SetupLogger_LogCompleted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' completed for Username '{_requestUsername}'.");

    private void SetupUserRepository_ExistsAsync()
        => _userRepositoryMock
            .Setup(x => x.ExistsAsync(
                It.Is<string>(y => y == _requestUsername),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUserExistsResult)
            .Verifiable(Times.Once);

    private void SetupHashingService_Hash()
        => _hashingServiceMock
            .Setup(x => x.Hash(
                It.Is<string>(y => y == _requestPassword)))
            .Returns(_testPasswordHash)
            .Verifiable(Times.Once);

    private void SetupUserRepository_Add()
        => _userRepositoryMock
            .Setup(x => x.Add(
                It.Is<User>(y => y.Username == _requestUsername && y.PasswordHash == _testPasswordHash)))
            .Returns(_testAddedUser)
            .Verifiable(Times.Once);
}
