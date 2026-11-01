namespace DannyGoodacre.Cqrs.Tests;

public sealed class QueryValidationDecoratorTests
{
    public sealed class Query : IQuery<string>;

    private readonly QueryValidationTestContext<Query, string> _context = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_DelegatesToInnerHandlerAndExecutesValidationPipeline()
    {
        // Arrange
        var query = new Query();

        const string expectedResponse = "Response";

        var expectedResult = Result<string>.Success(expectedResponse);

        _context.ValidatorMock
            .Setup(x => x.Validate(query, It.IsAny<ValidationState>()));

        _context.InnerMock
            .Setup(x => x.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var pipeline = _context.CreatePipeline(includeValidator: true);

        // Act
        IResult<string> result = await pipeline.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess(expectedResponse);

        _context.ValidatorMock.Verify(x => x.Validate(query, It.IsAny<ValidationState>()), Times.Once);
    }
}
