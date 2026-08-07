using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class DeleteUserTests : StateCommandHandlerTestBase<DeleteUserHandler>
{
    protected override string CommandName => "Delete User";

    private Guid _requestId;

    private User _testUser = null!;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        CommandHandler = new DeleteUserHandler(LoggerMock.Object, StateUnitMock.Object, _userRepositoryMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestId, TestCancellationToken);

    [Test]
    public async Task WhenIdInvalid_ShouldReturnInvalid()
    {
        // Arrange
        _requestId = Guid.Empty;

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Id:{Environment.NewLine}  - Must not be empty.");

        // Act
        Result result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenUserIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestId = Guid.NewGuid();

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = null!;

        SetupUserRepository_GetAsync();

        SetupLogger_LogNotFound();

        // Act
        Result result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenUserIsFound_ShouldReturnSuccess()
    {
        // Arrange
        _requestId = Guid.NewGuid();

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = "Test Username",
            IsApproved = true,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_GetAsync();

        SetupUserRepository_Remove();

        SetupLogger_LogCompleted();

        SetupStateUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupLogger_LogStarted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' started for User ID '{_requestId}'.");

    private void SetupLogger_LogNotFound()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed: User ID '{_requestId}' not found.");

    private void SetupLogger_LogCompleted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' completed for User ID '{_requestId}'.");

    private void SetupUserRepository_GetAsync()
        => _userRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<Guid>(y => y == _requestId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);

    private void SetupUserRepository_Remove()
        => _userRepositoryMock
            .Setup(x => x.Remove(
                It.Is<User>(y => y == _testUser)))
            .Verifiable(Times.Once);
}
