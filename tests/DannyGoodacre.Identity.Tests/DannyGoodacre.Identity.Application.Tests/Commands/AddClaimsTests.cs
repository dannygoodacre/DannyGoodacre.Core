using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Commands;

internal sealed class AddClaimsTests : StateCommandHandlerTestBase<AddClaimsHandler>
{
    protected override string CommandName => "Add Claims";

    private List<ClaimDefinition> _requestClaimDefinitions = null!;

    private List<Claim> _testClaims = null!;

    private Mock<IClaimRepository> _claimRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _claimRepositoryMock = new Mock<IClaimRepository>(MockBehavior.Strict);

        CommandHandler = new AddClaimsHandler(LoggerMock.Object, StateUnitMock.Object, _claimRepositoryMock.Object);
    }

    protected override Task<Result> Act()
        => CommandHandler.ExecuteAsync(_requestClaimDefinitions, TestCancellationToken);

    private readonly static TestCaseData[] _invalidClaimsTestCases =
    [
        new(null),
        new(new List<ClaimDefinition>())
    ];

    [TestCaseSource(nameof(_invalidClaimsTestCases))]
    public async Task WhenRequestInvalid_ShouldReturnInvalid(List<ClaimDefinition> claimDefinitions)
    {
        // Arrange
        _requestClaimDefinitions = claimDefinitions;

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Claims:{Environment.NewLine}  - Must not be null or empty.");

        // Act
        Result result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenAllClaimsExist_ShouldReturnSuccess()
    {
        // Arrange
        _requestClaimDefinitions =
        [
            new ClaimDefinition
            {
                Type = "Test Claim Type 1",
                Value = "Test  Claim Value 1",
            },
            new ClaimDefinition
            {
                Type = "Test Claim Type 2",
                Value = "Test  Claim Value 2",
            }
        ];

        _testClaims =
        [
            new Claim
            {
                Type = _requestClaimDefinitions[0].Type,
                Value = _requestClaimDefinitions[0].Value,
            },
            new Claim
            {
                Type = _requestClaimDefinitions[1].Type,
                Value = _requestClaimDefinitions[1].Value,
            }
        ];

        SetupClaimRepository_GetExistingAsync();

        SetupStateUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task WhenSomeClaimsExistAndSomeAreNew_ShouldReturnSuccess()
    {
        // Arrange
        _requestClaimDefinitions =
        [
            new ClaimDefinition
            {
                Type = "Test Claim Type 1",
                Value = "Test Claim Value 1",
            },
            new ClaimDefinition
            {
                Type = "Test Claim Type 2",
                Value = "Test Claim Value 2",
            },
            new ClaimDefinition
            {
                Type = "Test Claim Type 3",
                Value = "Test Claim Value 3"
            },
        ];

        _testClaims =
        [
            new Claim
            {
                Type = "Test Claim Type 3",
                Value = "Test Claim Value 3"
            },
            new Claim
            {
                Type = "Test Claim Type 4",
                Value = "Test Claim Value 4"
            },
            new Claim
            {
                Type = "Test Claim Type 5",
                Value = "Test Claim Value 5"
            }
        ];

        SetupClaimRepository_GetExistingAsync();

        SetupClaimRepository_Add(_requestClaimDefinitions[0]);

        SetupClaimRepository_Add(_requestClaimDefinitions[1]);

        SetupStateUnit_SaveChangesAsync();

        // Act
        Result result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupClaimRepository_GetExistingAsync()
        => _claimRepositoryMock
            .Setup(x => x.GetExistingAsync(
                It.Is<List<ClaimDefinition>>(y => y == _requestClaimDefinitions),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testClaims)
            .Verifiable(Times.Once);

    private void SetupClaimRepository_Add(ClaimDefinition claim)
        => _claimRepositoryMock
            .Setup(x => x.Add(
                It.Is<Claim>(y => y.Type == claim.Type && y.Value == claim.Value)))
            .Returns((Claim x) => x)
            .Verifiable(Times.Once);
}
