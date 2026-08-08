using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

[TestFixture]
internal sealed class AddRoleTests : StateCommandHandlerTestBase<AddRoleHandler>
{
    protected override string CommandName => "Add Role";

    private string _requestName = null!;

    private List<Guid> _requestClaimIds = null!;

    private AddRoleCommand _requestCommand = null!;

    private bool _testRoleExists;

    private Dictionary<Guid, int> _testClaimIdMap = null!;

    private List<Guid> _testMissingClaimIds = null!;

    private List<int> _testClaimIds = null!;

    private Mock<IRoleRepository> _roleRepositoryMock = null!;

    private Mock<IClaimRepository> _claimRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);

        _claimRepositoryMock = new Mock<IClaimRepository>(MockBehavior.Strict);

        CommandHandler = new AddRoleHandler(LoggerMock.Object, StateUnitMock.Object, _roleRepositoryMock.Object, _claimRepositoryMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestCommand, TestCancellationToken);

    private readonly static TestCaseData[] _nullOrEmptyRequestTestCases =
    [
        new(null, null),
        new("", new List<Guid>())
    ];

    [TestCaseSource(nameof(_nullOrEmptyRequestTestCases))]
    public async Task WhenNameIsNullOrWhitespaceAndClaimsIdIsNullOrEmpty_ShouldReturnInvalid(string name, List<Guid> claimIds)
    {
        // Arrange
        _requestName = name;

        _requestClaimIds = claimIds;

        _requestCommand = new AddRoleCommand
        {
            Name = _requestName,
            ClaimIds = _requestClaimIds
        };

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Name:{Environment.NewLine}  - Must not be null, empty, or whitespace.{Environment.NewLine}ClaimIds:{Environment.NewLine}  - Must not be null or empty.");

        // Act
        Result result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenClaimIdsContainsEmptyGuid_ShouldReturnInvalid()
    {
        // Arrange
        _requestName = "Request Name";

        _requestClaimIds = [Guid.Empty, Guid.NewGuid()];

        _requestCommand = new AddRoleCommand
        {
            Name = _requestName,
            ClaimIds = _requestClaimIds
        };

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"ClaimIds:{Environment.NewLine}  - Must not be empty.");

        // Act
        Result result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenRoleExists_ShouldReturnConflict()
    {
        // Arrange
        _requestName = "Request Name";

        _requestClaimIds = [Guid.NewGuid(), Guid.NewGuid()];

        _requestCommand = new AddRoleCommand
        {
            Name = _requestName,
            ClaimIds = _requestClaimIds
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testRoleExists = true;

        SetupRoleRepository_ExistsAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertConflict(result, "Role already exists");
    }

    [Test]
    public async Task WhenMissingClaims_ShouldReturnDomainError()
    {
        // Arrange
        _requestName = "Request Name";

        _requestClaimIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        _requestCommand = new AddRoleCommand
        {
            Name = _requestName,
            ClaimIds = _requestClaimIds
        };

        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testRoleExists = false;

        SetupRoleRepository_ExistsAsync();

        _testClaimIdMap = new Dictionary<Guid, int>
        {
            { _requestClaimIds[1], 123 },
            { _requestClaimIds[2], 456 }
        };

        SetupClaimRepository_GetIdMapAsync();

        _testMissingClaimIds = [_requestClaimIds[0], _requestClaimIds[3]];

        SetupLogger_LogMissingClaims();

        string expectedDomainErrorMessage = $"Missing claims: {_requestClaimIds[0]}, {_requestClaimIds[3]}.";

        // Act
        Result result = await Act();

        // Assert
        AssertDomainError(result, expectedDomainErrorMessage);
    }

    [Test]
    public async Task WhenNoMissingClaims_ShouldReturnSuccess()
    {
        // Arrange
        _requestName = "Request Name";

        _requestClaimIds = [Guid.NewGuid(), Guid.NewGuid()];

        _requestCommand = new AddRoleCommand
        {
            Name = _requestName,
            ClaimIds = _requestClaimIds
        };


        SetupLogger_IsEnabled();

        SetupLogger_LogStarted();

        _testRoleExists = false;

        SetupRoleRepository_ExistsAsync();

        _testClaimIdMap = new Dictionary<Guid, int>
        {
            { _requestClaimIds[0], 123 },
            { _requestClaimIds[1], 456 }
        };

        SetupClaimRepository_GetIdMapAsync();

        _testClaimIds = [123, 456];

        SetupRoleRepository_Add();

        SetupLogger_LogCompleted();

        SetupStateUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupLogger_LogStarted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' started for Role '{_requestName}' and Claim IDs '{string.Join(", ", _requestClaimIds)}'.");

    private void SetupLogger_LogMissingClaims()
        => LoggerMock.Setup(LogLevel.Warning, $"Command '{CommandName}' failed for Role '{_requestName}': missing Claims with IDs '{string.Join(", ", _testMissingClaimIds)}'.");

    private void SetupLogger_LogCompleted()
        => LoggerMock.Setup(LogLevel.Information, $"Command '{CommandName}' completed for Role '{_requestName}' and Claim IDs '{string.Join(", ", _requestClaimIds)}'.");

    private void SetupRoleRepository_ExistsAsync()
        => _roleRepositoryMock
            .Setup(x => x.ExistsAsync(
                It.Is<string>(y => y == _requestName),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testRoleExists)
            .Verifiable(Times.Once);

    private void SetupClaimRepository_GetIdMapAsync()
        => _claimRepositoryMock
            .Setup(x => x.GetIdMapAsync(
                It.Is<List<Guid>>(y => y == _requestClaimIds),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testClaimIdMap)
            .Verifiable(Times.Once);

    private void SetupRoleRepository_Add()
        => _roleRepositoryMock
            .Setup(x => x.Add(
                It.Is<Role>(y =>
                    y.Name == _requestName &&
                    y.Claims.Select(claim => claim.ClaimId).SequenceEqual(_testClaimIds))))
            .Returns((Role x) => x)
            .Verifiable(Times.Once);
}
