using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

[TestFixture]
internal sealed class GetClaimTests : QueryHandlerTestBase<GetClaimHandler, ClaimResponse>
{
    protected override string QueryName => "Get Claim";

    private Guid _requestClaimId;

    private Claim? _testClaim;

    private Mock<IClaimRepository> _claimRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _claimRepositoryMock = new Mock<IClaimRepository>(MockBehavior.Strict);

        QueryHandler = new GetClaimHandler(LoggerMock.Object, _claimRepositoryMock.Object);
    }

    protected override Task<Result<ClaimResponse>> Act()
        => QueryHandler.ExecuteAsync(_requestClaimId, TestCancellationToken);

    [TestCase("00000000-0000-0000-0000-000000000000")]
    public async Task WhenClaimIdInvalid_ShouldReturnInvalid(string userId)
    {
        // Arrange
        _requestClaimId = Guid.Parse(userId);

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Id:{Environment.NewLine}  - Must not be empty.");

        // Act
        Result<ClaimResponse> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenClaimIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestClaimId = Guid.NewGuid();

        _testClaim = null;

        SetupClaimRepository_GetAsync();

        // Act
        Result<ClaimResponse> result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenClaimExists_ShouldReturnSuccess()
    {
        // Arrange
        _requestClaimId = Guid.NewGuid();

        var testPublicId = Guid.NewGuid();

        const string testClaimType = "Test Claim Type";

        const string testClaimValue = "Test Claim Value";

        _testClaim = new Claim
        {
            Id = 123,
            PublicId = testPublicId,
            Type = testClaimType,
            Value = testClaimValue
        };

        var expectedClaimResponse = new ClaimResponse
        {
            Id = testPublicId,
            Type = testClaimType,
            Value = testClaimValue
        };

        SetupClaimRepository_GetAsync();

        // Act
        Result<ClaimResponse> result = await Act();

        // Assert
        AssertSuccess(result, expectedClaimResponse);
    }

    private void SetupClaimRepository_GetAsync()
        => _claimRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<Guid>(y => y == _requestClaimId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testClaim)
            .Verifiable(Times.Once);
}
