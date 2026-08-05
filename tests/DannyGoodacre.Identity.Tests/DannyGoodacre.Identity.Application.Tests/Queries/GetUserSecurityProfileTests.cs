using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Domain;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

[TestFixture]
internal sealed class GetUserSecurityProfileTests : QueryHandlerTestBase<GetUserSecurityProfileHandler, UserSecurityProfileResponse>
{
    protected override string QueryName => "Get User Security Profile";

    private string _requestUsername = null!;

    private int _testUserId;

    private User _testUser = null!;

    private List<Claim> _testClaims = null!;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    private Mock<IUserClaimRepository> _userClaimRepository = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        _userClaimRepository = new Mock<IUserClaimRepository>(MockBehavior.Strict);

        QueryHandler = new GetUserSecurityProfileHandler(LoggerMock.Object, _userRepositoryMock.Object, _userClaimRepository.Object);
    }

    protected override Task<Result<UserSecurityProfileResponse>> Act()
        => QueryHandler.ExecuteAsync(_requestUsername, TestCancellationToken);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task WhenUsernameInvalid_ShouldReturnInvalid(string username)
    {
        // Arrange
        _requestUsername = username;

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Username:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        // Act
        Result<UserSecurityProfileResponse> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenUserIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestUsername = "Request Username";

        _testUser = null!;

        SetupUserRepository_GetAsync();

        // Act
        Result<UserSecurityProfileResponse> result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenUserExists_ShouldReturnSuccess()
    {
        // Arrange
        _requestUsername = "Request Username";

        _testUserId = 123;

        var testPublicId = Guid.NewGuid();

        const string testUsername = "Test Username";

        var testRoleNames = new List<string>
        {
            "Test Role Name 1",
            "Test Role Name 2"
        };

        var testUserRoles = new List<UserRole>
        {
            new()
            {
                RoleId = 456,
                Role = new Role
                {
                    Name = testRoleNames[0]
                }
            },
            new()
            {
                RoleId = 789,
                Role = new Role
                {
                    Name = testRoleNames[1]
                }
            }
        };

        _testUser = new User
        {
            Id = _testUserId,
            PublicId = testPublicId,
            Username = testUsername,
            IsApproved = true,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp",
            Roles = testUserRoles
        };

        SetupUserRepository_GetAsync();

        _testClaims =
        [
            new Claim
            {
                PublicId = Guid.NewGuid(),
                Type = "Test Claim Type 1",
                Value = "Test Claim Value 1",
            },
            new Claim
            {
                PublicId = Guid.NewGuid(),
                Type = "Test Claim Type 2",
                Value = "Test Claim Value 2",
            }
        ];

        SetupUserClaimRepository_GetManyAsync();

        var expectedClaimDefinitions = new List<ClaimDefinition>
        {
            new()
            {
                Type = _testClaims[0].Type,
                Value = _testClaims[0].Value
            },
            new()
            {
                Type = _testClaims[1].Type,
                Value = _testClaims[1].Value
            }
        };

        var expectedUserSecurityProfileResponse = new UserSecurityProfileResponse
        {
            Id = testPublicId,
            Username = testUsername,
            Claims = expectedClaimDefinitions,
            Roles = testRoleNames
        };

        // Act
        Result<UserSecurityProfileResponse> result = await Act();

        // Assert
        AssertSuccess(result, expectedUserSecurityProfileResponse);
    }

    private void SetupUserRepository_GetAsync()
        => _userRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<string>(y => y == _requestUsername),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);

    private void SetupUserClaimRepository_GetManyAsync()
        => _userClaimRepository
            .Setup(x => x.GetManyAsync(
                It.Is<int>(y => y == _testUserId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testClaims)
            .Verifiable(Times.Once);
}
