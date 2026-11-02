using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class CreateUserPipelineTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);

    private readonly Mock<IStateUnit> _stateUnitMock = new(MockBehavior.Strict);

    private readonly Mock<IEmailService> _emailServiceMock = new(MockBehavior.Strict);

    private ICommandHandler<CreateUserCommand, Guid> BuildPipeline()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(_userRepositoryMock.Object);
        services.AddSingleton(_stateUnitMock.Object);
        services.AddSingleton(_emailServiceMock.Object);

        services.AddCqrsHandlers(typeof(CreateUserCommand).Assembly);

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<ICommandHandler<CreateUserCommand, Guid>>();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ExecutesHandlerSavesStateAndFiresPostCommitHook()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var command = new CreateUserCommand("user@example.com", "User Name");

        _userRepositoryMock
            .Setup(x => x.Add(It.Is<User>(u => u.Email == command.Email && u.Name == command.Name)));

        _stateUnitMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _emailServiceMock
            .Setup(x => x.SendWelcomeEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        Guid userId = result.ShouldBeSuccess();

        Assert.NotEqual(Guid.Empty, userId);

        // Verify ordering & execution
        _userRepositoryMock.Verify(x => x.Add(It.IsAny<User>()), Times.Once);

        _stateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShortCircuitsBeforeHandlerStateUnitOrHookExecute()
    {
        // Arrange
        var pipeline = BuildPipeline();

        var invalidCommand = new CreateUserCommand("invalid-email", "");

        // Act
        IResult<Guid> result = await pipeline.HandleAsync(invalidCommand, CancellationToken.None);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal(2, validationState.Errors.Count);
        Assert.Contains("Email", validationState.Errors.Keys);
        Assert.Contains("Name", validationState.Errors.Keys);

        // Verify
        _userRepositoryMock.Verify(x => x.Add(It.IsAny<User>()), Times.Never);

        _stateUnitMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
