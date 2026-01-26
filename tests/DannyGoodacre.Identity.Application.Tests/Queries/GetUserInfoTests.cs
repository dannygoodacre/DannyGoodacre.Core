using DannyGoodacre.Core;
using DannyGoodacre.Identity.Application.Abstractions;
using DannyGoodacre.Identity.Application.Commands;
using DannyGoodacre.Identity.Application.Models;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Core;
using Microsoft.Extensions.Logging;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

[TestFixture]
internal class GetUserInfoTests : QueryHandlerTestBase<GetUserInfoHandler, UserInfoResponse>
{

    protected override string QueryName => "Get User Information";

    private string _requestUserId = null!;

    private IdentityUser _testUser = null!;

    private const string TestUsername = "Test Username";

    private const bool TestIsApproved = false;

    private UserInfoResponse _testUserInfoResponse = null!;

    private Mock<IUserManager<IdentityUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _requestUserId = "Request User Id";

        _userManagerMock = new Mock<IUserManager<IdentityUser>>(MockBehavior.Strict);

        _testUser = new IdentityUser()
        {
            Id = _requestUserId,
            UserName = TestUsername,
            EmailConfirmed = TestIsApproved
        };

        _testUserInfoResponse = new UserInfoResponse
        {
            Id = _requestUserId,
            Username = TestUsername,
            IsApproved = TestIsApproved
        };

        QueryHandler = new GetUserInfoHandler(LoggerMock.Object, _userManagerMock.Object);
    }

    protected override Task<Result<UserInfoResponse>> Act()
        => QueryHandler.ExecuteAsync(_requestUserId, CancellationToken);

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task ApproveUser_WhenUserIdInvalid_ShouldReturnInvalid(string userId)
    {
        // Arrange
        _requestUserId = userId;

        LoggerMock.Setup(LogLevel.Error, $"Command '{QueryName}' failed validation: UserId:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        // Act
        var result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task ApproveUser_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _testUser = null!;

        SetupUserManager_FindByIdAsync();

        // Act
        var result = await Act();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public async Task ApproveUser_WhenSuccessful_ShouldReturnUserInfo()
    {
        // Arrange
        SetupUserManager_FindByIdAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result, _testUserInfoResponse);
    }

    private void SetupUserManager_FindByIdAsync()
        => _userManagerMock
            .Setup(x => x.FindByIdAsync(
                It.Is<string>(y => y == _requestUserId)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);
}
