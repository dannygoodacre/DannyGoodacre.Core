using Microsoft.Extensions.Logging.Abstractions;

namespace DannyGoodacre.Cqrs.Tests;

public sealed class MultipleValidatorsTests
{
    public sealed class Command : ICommand;

    [Fact]
    public async Task HandleAsync_WhenTwoValidatorsExist_RunsBothAndReturnsAllErrors()
    {
        // Arrange
        var innerMock = new Mock<ICommandHandler<Command>>(MockBehavior.Strict);

        var firstValidatorMock = new Mock<IValidator<Command>>(MockBehavior.Strict);

        var secondValidatorMock = new Mock<IValidator<Command>>(MockBehavior.Strict);

        firstValidatorMock
            .Setup(x => x.Validate(It.IsAny<Command>(), It.IsAny<ValidationState>()))
            .Callback((Command _, ValidationState state) => state.AddError("Name", "Name is required."));

        secondValidatorMock
            .Setup(x => x.Validate(It.IsAny<Command>(), It.IsAny<ValidationState>()))
            .Callback((Command _, ValidationState state) => state.AddError("Email", "Email is required."));

        var pipeline = new CommandValidationDecorator<Command>(
            innerMock.Object,
            NullLogger<Command>.Instance,
            [firstValidatorMock.Object, secondValidatorMock.Object]);

        // Act
        IResult result = await pipeline.HandleAsync(new Command(), CancellationToken.None);

        // Assert
        ValidationState validationState = result.ShouldBeInvalid();

        Assert.Equal(["Name", "Email"], validationState.Errors.Keys);

        innerMock.Verify(x => x.HandleAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
