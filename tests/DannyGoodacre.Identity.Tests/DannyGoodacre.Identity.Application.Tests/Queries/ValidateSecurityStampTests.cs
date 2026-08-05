using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;
using DannyGoodacre.Identity.Application.Queries;
using DannyGoodacre.Identity.Entities;
using DannyGoodacre.Primitives;

namespace DannyGoodacre.Identity.Application.Tests.Queries;

[TestFixture]
internal sealed class ValidateSecurityStampTests : QueryHandlerTestBase<ValidateSecurityStampHandler, bool>
{
    protected override string QueryName => "Validate Security Stamp";

    private string _requestUsername = null!;

    private string _requestSecurityStamp = null!;

    private User _testUser = null!;

    private Mock<IUserRepository> _userRepositoryMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

        QueryHandler = new ValidateSecurityStampHandler(LoggerMock.Object, _userRepositoryMock.Object);
    }

    protected override Task<Result<bool>> Act()
        => QueryHandler.ExecuteAsync(_requestUsername, _requestSecurityStamp, TestCancellationToken);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task WhenRequestInvalid_ShouldReturnInvalid(string username)
    {
        // Arrange
        _requestUsername = username;

        _requestSecurityStamp = "Test Security Stamp";

        SetupLogger_IsEnabled();

        SetupLogger_FailedValidation($"Username:{Environment.NewLine}  - Must not be null, empty, or whitespace.");

        // Act
        Result<bool> result = await Act();

        // Assert
        AssertInvalid(result);
    }

    [Test]
    public async Task WhenUserIsNull_ShouldReturnSuccessAndFalse()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestSecurityStamp = "Request Security Stamp";

        _testUser = null!;

        SetupUserRepository_GetAsync();

        const bool expectedResult = false;

        // Act
        Result<bool> result = await Act();

        // Assert
        AssertSuccess(result, expectedResult);
    }

    [Test]
    public async Task WhenSecurityStampDoesNotMatch_ShouldReturnSuccessAndFalse()
    {
        // Arrange
        _requestUsername = "Request Username";

        _requestSecurityStamp = "Request Security Stamp";

        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = "Test Username",
            IsApproved = true,
            PasswordHash = "Test Password Hash",
            SecurityStamp = "Test Security Stamp",
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_GetAsync();

        const bool expectedResult = false;

        // Act
        Result<bool> result = await Act();

        // Assert
        AssertSuccess(result, expectedResult);
    }

    [Test]
    public async Task WhenSecurityStampMatches_ShouldReturnSuccessAndTrue()
    {
        // Arrange
        _requestUsername = "Request Username";

        const string testSecurityStamp = "Test Security Stamp";

        _requestSecurityStamp = testSecurityStamp;

        _testUser = new User
        {
            PublicId = Guid.NewGuid(),
            Username = "Test Username",
            IsApproved = true,
            PasswordHash = "Test Password Hash",
            SecurityStamp = testSecurityStamp,
            ConcurrencyStamp = "Test Concurrency Stamp"
        };

        SetupUserRepository_GetAsync();

        const bool expectedResult = true;

        // Act
        Result<bool> result = await Act();

        // Assert
        AssertSuccess(result, expectedResult);
    }

    private void SetupUserRepository_GetAsync()
        => _userRepositoryMock
            .Setup(x => x.GetAsync(
                It.Is<string>(y => y == _requestUsername),
                It.Is<CancellationToken>(y => y == TestCancellationToken)))
            .ReturnsAsync(_testUser)
            .Verifiable(Times.Once);
}
