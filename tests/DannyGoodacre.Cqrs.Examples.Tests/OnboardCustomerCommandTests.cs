using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;

namespace DannyGoodacre.Cqrs.Examples.Tests;

public sealed class OnboardCustomerCommandHandlerTests
{
    private readonly Mock<ICommandHandler<CreateCustomerCommand, int>> _createCustomerMock = new(MockBehavior.Strict);
    private readonly Mock<IQueryHandler<GetWelcomeEmailTemplateQuery, string>> _getTemplateMock = new(MockBehavior.Strict);
    private readonly Mock<ICommandHandler<ScheduleEmailCommand>> _scheduleEmailMock = new(MockBehavior.Strict);

    private OnboardCustomerCommandHandler CreateHandler() => new(
        _createCustomerMock.Object,
        _getTemplateMock.Object,
        _scheduleEmailMock.Object);

    [Fact]
    public async Task HandleAsync_WhenAllStepsSucceed_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "en-US");

        const int expectedCustomerId = 42;
        const string expectedTemplate = "Welcome to our platform!";

        _createCustomerMock
            .Setup(x => x.HandleAsync(new CreateCustomerCommand(command.Name, command.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(expectedCustomerId));

        _getTemplateMock
            .Setup(x => x.HandleAsync(new GetWelcomeEmailTemplateQuery(command.LanguageCode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(expectedTemplate));

        _scheduleEmailMock
            .Setup(x => x.HandleAsync(new ScheduleEmailCommand(expectedCustomerId, expectedTemplate), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        IResult result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();

        _createCustomerMock.Verify(x => x.HandleAsync(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _scheduleEmailMock.Verify(x => x.HandleAsync(It.IsAny<ScheduleEmailCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenCreateCustomerFails_ShortCircuitsAndReturnsFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "en-US");

        var expectedError = Result<int>.DomainError("Email already in use.");

        _createCustomerMock
            .Setup(x => x.HandleAsync(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        // Act
        IResult result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeDomainError("Email already in use.");

        _createCustomerMock.Verify(x => x.HandleAsync(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify
        _getTemplateMock.Verify(x => x.HandleAsync(It.IsAny<GetWelcomeEmailTemplateQuery>(), It.IsAny<CancellationToken>()), Times.Never);

        _scheduleEmailMock.Verify(x => x.HandleAsync(It.IsAny<ScheduleEmailCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenGetTemplateFails_ShortCircuitsAndReturnsFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new OnboardCustomerCommand("Jane Doe", "jane@example.com", "INVALID_LANG");

        const int expectedCustomerId = 42;

        _createCustomerMock
            .Setup(x => x.HandleAsync(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(expectedCustomerId));

        _getTemplateMock
            .Setup(x => x.HandleAsync(It.IsAny<GetWelcomeEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.NotFound());

        // Act
        IResult result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBeNotFound();

        _createCustomerMock.Verify(x => x.HandleAsync(It.IsAny<CreateCustomerCommand>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify
        _scheduleEmailMock.Verify(x => x.HandleAsync(It.IsAny<ScheduleEmailCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
