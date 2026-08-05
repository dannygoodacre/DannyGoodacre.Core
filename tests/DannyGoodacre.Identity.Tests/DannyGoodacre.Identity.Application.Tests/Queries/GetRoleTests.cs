using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

[TestFixture]
internal sealed class GetRoleTests : QueryHandlerTestBase<GetRoleHandler, RoleResponse>
{
    protected override string QueryName => "Get Role";

    private Guid _requestRoleId;

    private Role? _testRole;

    private Mock<IRoleRepository> _roleRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);

        QueryHandler = new GetRoleHandler(LoggerMock.Object, _roleRepositoryMock.Object);
    }

    protected override Task<Result<RoleResponse>> Act()
        => QueryHandler.ExecuteAsync(_requestRoleId, TestCancellationToken);

    [TestCase("00000000-0000-0000-0000-000000000000")]
    public async Task WhenRoleIdInvalid_ShouldReturnInvalid(string userId)
    {
        // Arrange
        _requestRoleId = Guid.Parse(userId);

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Id:{Environment.NewLine}  - Must not be empty.");

        // Act
        Result<RoleResponse> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenRoleIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestRoleId = Guid.NewGuid();

        _testRole = null;

        SetupRoleRepository_GetAsync();

        // Act
        Result<RoleResponse> result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenRoleExists_ShouldReturnSuccess()
    {
        // Arrange
        _requestRoleId = Guid.NewGuid();

        Guid testPublicId = Guid.NewGuid();

        const string testRoleName = "Test Role Name";

        List<Claim> testClaims =
        [
            new()
            {
                Id = 123,
                PublicId = Guid.NewGuid(),
                Type = "Test Claim Type 1",
                Value = "Test Claim Value 1"
            },
            new()
            {
                Id = 456,
                PublicId = Guid.NewGuid(),
                Type = "Test Claim Type 2",
                Value = "Test Claim Value 2"
            }
        ];

        List<RoleClaim> testRoleClaims =
        [
            new()
            {
                Id = 789,
                Claim = testClaims[0]
            },
            new()
            {
                Id = 101,
                Claim = testClaims[1]
            }
        ];

        _testRole = new Role
        {
            Id = 123,
            PublicId = testPublicId,
            Name = testRoleName,
            Claims = testRoleClaims
        };

        var expectedRoleResponse = new RoleResponse
        {
            Id = testPublicId,
            Name = testRoleName,
            Claims = testRoleClaims.Select(x =>
                new ClaimResponse
                {
                    Id = x.Claim.PublicId,
                    Type = x.Claim.Type,
                    Value = x.Claim.Value
                }).ToList()
        };

        SetupRoleRepository_GetAsync();

        // Act
        Result<RoleResponse> result = await Act();

        // Assert
        AssertSuccess(result, expectedRoleResponse);
    }

    private void SetupRoleRepository_GetAsync()
        => _roleRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<Guid>(y => y == _requestRoleId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testRole)
            .Verifiable(Times.Once);
}
