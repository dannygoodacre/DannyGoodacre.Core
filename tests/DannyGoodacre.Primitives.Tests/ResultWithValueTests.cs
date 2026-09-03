namespace DannyGoodacre.Primitives.Tests;

[TestFixture]
public sealed class ResultWithValueTests : TestBase
{
    [Test]
    public void Success()
    {
        // Arrange
        const int testValue = 123;

        // Act
        IResult<int> result = Result<int>.Success(testValue);

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public void Canceled()
    {
        // Act
        IResult<int> result = Result<int>.Canceled();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string testMessage = "Test Conflict Message";

        // Act
        IResult<int> result = Result<int>.Conflict(testMessage);

        // Assert
        AssertConflict(result, testMessage);
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string testMessage = "Test Error Message";

        // Act
        IResult<int> result = Result<int>.DomainError(testMessage);

        // Assert
        AssertDomainError(result, testMessage);
    }

    [Test]
    public void InternalError_WithMessage()
    {
        // Act
        const string testMessage = "Test Error Message";

        IResult<int> result = Result<int>.InternalError(testMessage);

        // Assert
        AssertInternalError(result, testMessage);
    }

    [Test]
    public void InternalError_WithException()
    {
        // Arrange
        var testException = new Exception("Test Exception");

        // Act
        IResult<int> result = Result<int>.InternalError(testException);

        // Assert
        AssertInternalError(result, testException);
    }

    [Test]
    public void Invalid()
    {
        // Arrange
        var testValidationState = new ValidationState();

        const string testProperty1 = "Test Property 1";
        const string testProperty2 = "Test Property 2";

        const string testError1 = "Test Error 1";
        const string testError2 = "Test Error 2";

        testValidationState.AddError(testProperty1, testError1);
        testValidationState.AddError(testProperty2, testError2);

        // Act
        IResult<int> result = Result<int>.Invalid(testValidationState);

        // Assert
        AssertInvalid(result, testValidationState);
    }

    [Test]
    public void NotFound()
    {
        // Act
        IResult<int> result = Result<int>.NotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void MapFailure_WhenSuccess_ShouldThrowException()
    {
        // Arrange
        IResult<int> testResult = Result<int>.Success(123);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => testResult.MapFailure<int>());
    }

    [Test]
    public void MapFailure_WhenNotSuccess_ShouldReturnResult()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        IResult<int> testResult = Result<int>.InternalError(testErrorMessage);

        // Act
        IResult<string> result = testResult.MapFailure<string>();

        AssertInternalError(result, testErrorMessage);
    }
}
