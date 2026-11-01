using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class GetUserByIdQueryPipelineTests
{
    private readonly Mock<IUserRepository> _userReadRepositoryMock = new(MockBehavior.Strict);

    private IQueryHandler<GetUserByIdQuery, UserResponse> BuildPipeline()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(_userReadRepositoryMock.Object);

        services.AddCqrsHandlers(typeof(GetUserByIdQuery).Assembly);

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IQueryHandler<GetUserByIdQuery, UserResponse>>();
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_ReturnsSuccessWithUserResponse()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var userId = Guid.NewGuid();

        var query = new GetUserByIdQuery(userId);

        var expectedUser = new User(userId, "john.doe@example.com", "John Doe");

        var expectedUserResponse = expectedUser.ToResponse();

        _userReadRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        IResult<UserResponse> result = await pipeline.HandleAsync(query, CancellationToken.None);

        // Assert
        UserResponse response = result.ShouldBeSuccess();

        Assert.Equal(expectedUserResponse, response);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var userId = Guid.NewGuid();

        var query = new GetUserByIdQuery(userId);

        _userReadRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        IResult<UserResponse> result = await pipeline.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldBeNotFound();
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShortCircuitsWithoutCallingRepository()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var invalidQuery = new GetUserByIdQuery(Guid.Empty);

        // Act
        IResult<UserResponse> result = await pipeline.HandleAsync(invalidQuery, CancellationToken.None);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Single(validationState.Errors);
        Assert.Contains("UserId", validationState.Errors.Keys);

        // Verify
        _userReadRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
