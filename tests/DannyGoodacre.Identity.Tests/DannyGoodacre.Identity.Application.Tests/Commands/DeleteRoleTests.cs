using DannyGoodacre.Cqrs;
using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class DeleteRoleTests : StateCommandHandlerTestBase<DeleteRoleHandler>
{
    protected override string CommandName => "Delete Role";

    private Guid _requestId;

    private Role _testRole = null!;

    private Mock<IRoleRepository> _roleRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);

        CommandHandler = new DeleteRoleHandler(LoggerMock.Object, StateUnitMock.Object, _roleRepositoryMock.Object);
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
    public async Task WhenRoleIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestId = Guid.NewGuid();

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testRole = null!;

        SetupRoleRepository_GetAsync();

        SetupLogger_LogNotFound();

        // Act
        Result result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenRoleIsFound_ShouldReturnSuccess()
    {
        // Arrange
        _requestId = Guid.NewGuid();

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testRole = new Role
        {
            Name = "Test Role Name"
        };

        SetupRoleRepository_GetAsync();

        SetupRoleRepository_Remove();

        SetupLogger_LogCompleted();

        SetupStateUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupLogger_LogStarted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' started for Role ID '{_requestId}'.");

    private void SetupLogger_LogNotFound()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed: Role ID '{_requestId}' not found.");

    private void SetupLogger_LogCompleted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' completed for Role ID '{_requestId}'.");

    private void SetupRoleRepository_GetAsync()
        => _roleRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<Guid>(y => y == _requestId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testRole)
            .Verifiable(Times.Once);

    private void SetupRoleRepository_Remove()
        => _roleRepositoryMock
            .Setup(x => x.Remove(
                It.Is<Role>(y => y == _testRole)))
            .Verifiable(Times.Once);
}
