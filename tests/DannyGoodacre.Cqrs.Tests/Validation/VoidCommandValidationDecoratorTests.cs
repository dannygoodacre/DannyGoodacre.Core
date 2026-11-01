namespace DannyGoodacre.Cqrs.Tests;

public sealed class VoidCommandValidationDecoratorTests
{
    public sealed class Command : ICommand;

    private readonly CommandValidationTestContext<Command> _context = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WhenValidatorIsNull_DelegatesToInnerHandler()
    {
        // Arrange
        var command = new Command();

        var expectedResult = Result.Success();

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var pipeline = _context.CreatePipeline(includeValidator: false);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task HandleAsync_WhenValidatorHasNoErrors_DelegatesToInnerHandler()
    {
        // Arrange
        var command = new Command();

        var expectedResult = Result.Success();

        _context.ValidatorMock
            .Setup(x => x.Validate(command, It.IsAny<ValidationState>()));

        _context.InnerMock
            .Setup(x => x.HandleAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var pipeline = _context.CreatePipeline(includeValidator: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _context.ValidatorMock.Verify(x => x.Validate(command, It.IsAny<ValidationState>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenValidatorHasErrors_LogsValidationFailedAndReturnsInvalidWithoutCallingInnerHandler()
    {
        // Arrange
        var command = new Command();

        _context.ValidatorMock
            .Setup(x => x.Validate(command, It.IsAny<ValidationState>()))
            .Callback<Command, ValidationState>((_, state) => state.AddError("PropertyName", "Error message"));

        var pipeline = _context.CreatePipeline(includeValidator: true);

        // Act
        IResult result = await pipeline.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeInvalid();

        _context.LogCollector.ShouldHaveLogged(CqrsLogEvent.RequestValidationFailed, LogLevel.Warning);

        _context.InnerMock.Verify(x => x.HandleAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
