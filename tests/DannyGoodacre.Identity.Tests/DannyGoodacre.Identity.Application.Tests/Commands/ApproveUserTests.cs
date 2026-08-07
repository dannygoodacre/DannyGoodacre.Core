using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class ApproveUserTests : StateCommandHandlerTestBase<ApproveUserHandler>
{
    protected override string CommandName => "Approve User";

    private Guid _requestId;

    private User _testUser = null!;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        CommandHandler = new ApproveUserHandler(LoggerMock.Object, StateUnitMock.Object, _userRepositoryMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestId, TestCancellationToken);

    [Test]
    public async Task WhenIdIsInvalid_ShouldReturnInvalid()
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

        SetupUserRepository_GetWithTrackingAsync();

        SetupLogger_LogNotFound();

        // Act
        Result result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenUserFound_ShouldReturnSuccess()
    {
        // Arrange
        _requestId = Guid.NewGuid();

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testUser = new User
        {
            PublicId = _requestId,
            Username = "Test Username",
            IsApproved = false,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_GetWithTrackingAsync();

        SetupStateUnit_SaveChangesAsync();

        SetupLogger_LogCompleted();

        // Act
        Result result = await Act();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_testUser.IsApproved, Is.True);

            AssertSuccess(result);
        }
    }

    private void SetupLogger_LogStarted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' started for User ID '{_requestId}'.");

    private void SetupLogger_LogNotFound()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed: User with ID '{_requestId}' not found.");

    private void SetupLogger_LogCompleted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' completed for User ID '{_requestId}'.");

    private void SetupUserRepository_GetWithTrackingAsync()
        => _userRepositoryMock
            .Setup(x => x.GetWithTrackingAsync(
                It.Is<Guid>(y => y == _requestId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);
}
