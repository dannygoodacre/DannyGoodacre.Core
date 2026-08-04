using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

[TestFixture]
internal sealed class GetUserTests : QueryHandlerTestBase<GetUserHandler, UserInfoResponse>
{
    protected override string QueryName => "Get User";

    private Guid _requestUserId;

    private User? _testUser;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        QueryHandler = new GetUserHandler(LoggerMock.Object, _userRepositoryMock.Object);
    }

    protected override Task<Result<UserInfoResponse>> Act()
        => QueryHandler.ExecuteAsync(_requestUserId, TestCancellationToken);

    [TestCase("00000000-0000-0000-0000-000000000000")]
    public async Task WhenUserIdInvalid_ShouldReturnInvalid(string userId)
    {
        // Arrange
        _requestUserId = Guid.Parse(userId);

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Id:{Environment.NewLine}  - Must not be empty.");

        // Act
        Result<UserInfoResponse> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenUserIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _requestUserId = Guid.NewGuid();

        _testUser = null;

        SetupUserRepository_GetAsync();

        // Act
        Result<UserInfoResponse> result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task WhenUserExists_ShouldReturnSuccess()
    {
        // Arrange
        _requestUserId = Guid.NewGuid();

        Guid testPublicId = Guid.NewGuid();

        const string testUsername = "Test Username";

        const bool testIsApproved = true;

        _testUser = new User
        {
            Id = 123,
            PublicId = testPublicId,
            Username = testUsername,
            IsApproved = testIsApproved,
            PasswordHash = null!,
            SecurityStamp = null!,
            ConcurrencyStamp = null!
        };

        var expectedUserInfoResponse = new UserInfoResponse
        {
            Id = testPublicId,
            Username = testUsername,
            IsApproved = testIsApproved
        };

        SetupUserRepository_GetAsync();

        // Act
        Result<UserInfoResponse> result = await Act();

        // Assert
        AssertSuccess(result, expectedUserInfoResponse);
    }

    private void SetupUserRepository_GetAsync()
        => _userRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<Guid>(y => y == _requestUserId),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);
}
