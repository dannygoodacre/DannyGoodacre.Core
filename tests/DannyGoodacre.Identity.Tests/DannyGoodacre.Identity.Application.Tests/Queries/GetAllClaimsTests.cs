using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

internal sealed class GetAllClaimsTests : QueryHandlerTestBase<GetAllClaimsHandler, List<ClaimResponse>>
{
    protected override string QueryName => "Get All Claims";

    private List<Claim> _testClaims = null!;

    private Mock<IClaimRepository> _claimRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _claimRepositoryMock = new Mock<IClaimRepository>(MockBehavior.Strict);

        QueryHandler = new GetAllClaimsHandler(LoggerMock.Object, _claimRepositoryMock.Object);
    }

    protected override Task<Result<List<ClaimResponse>>> Act()
        => QueryHandler.ExecuteAsync(TestCancellationToken);

    [Test]
    public async Task WhenNoClaimsFound_ShouldReturnSuccessWithEmptyList()
    {
        // Arrange
        _testClaims = [];

        var expectedClaimResponses = new List<ClaimResponse>();

        SetupClaimRepository_GetAllAsync();

        // Act
        Result<List<ClaimResponse>> result = await Act();

        // Assert
        AssertSuccess(result, expectedClaimResponses);
    }

    [Test]
    public async Task WhenClaimsFound_ShouldReturnSuccessWithClaims()
    {
        // Arrange
        _testClaims =
        [
            new Claim
            {
                PublicId = Guid.NewGuid(),
                Type = "Test Claim Type 1",
                Value = "Test Claim Value 1"
            },
            new Claim
            {
                PublicId = Guid.NewGuid(),
                Type = "Test Claim Type 2",
                Value = "Test Claim Value 2"
            }
        ];

        SetupClaimRepository_GetAllAsync();

        var expectedClaimResponses = new List<ClaimResponse>
        {
            new()
            {
                Id = _testClaims[0].PublicId,
                Type = _testClaims[0].Type,
                Value = _testClaims[0].Value
            },
            new()
            {
                Id = _testClaims[1].PublicId,
                Type = _testClaims[1].Type,
                Value = _testClaims[1].Value
            }
        };

        // Act
        Result<List<ClaimResponse>> result = await Act();

        // Assert
        AssertSuccess(result, expectedClaimResponses);
    }

    private void SetupClaimRepository_GetAllAsync()
        => _claimRepositoryMock
            .Setup(x => x.GetAllAsync(
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testClaims)
            .Verifiable(Times.Once);
}
