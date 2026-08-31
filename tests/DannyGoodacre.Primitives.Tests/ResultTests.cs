namespace DannyGoodacre.Primitives.Tests;

[TestFixture]
public sealed class ResultTests : TestBase
{
    [Test]
    public void Success()
    {
        // Act
        IResult result = Result.Success();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public void Success_WithImplicitValue()
    {
        // Arrange
        const int value = 123;

        // Act
        IResult<int> result = Result.Success(value);

        // Assert
        AssertSuccess(result, value);
    }

    [Test]
    public void Canceled()
    {
        // Act
        IResult result = Result.Canceled();

        // Assert
        AssertCanceled(result);
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string message = "Test Conflict Message";

        // Act
        IResult result = Result.Conflict(message);

        // Assert
        AssertConflict(result, message);
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string message = "Test Error Message";

        // Act
        IResult result = Result.DomainError(message);

        // Assert
        AssertDomainError(result, message);
    }

    [Test]
    public void InternalError_WithMessage()
    {
        // Act
        const string message = "Test Error Message";

        IResult result = Result.InternalError(message);

        // Assert
        AssertInternalError(result, message);
    }

    [Test]
    public void InternalError_WithException()
    {
        // Arrange
        var exception = new Exception("Test Exception");

        // Act
        IResult result = Result.InternalError(exception);

        // Assert
        AssertInternalError(result, exception);
    }

    [Test]
    public void Invalid()
    {
        // Arrange
        var validationState = new ValidationState();

        const string property1 = "Test Property 1";
        const string property2 = "Test Property 2";

        const string error1 = "Test Error 1";
        const string error2 = "Test Error 2";

        validationState.AddError(property1, error1);
        validationState.AddError(property2, error2);

        // Act
        IResult result = Result.Invalid(validationState);

        // Assert
        AssertInvalid(result, validationState);
    }

    [Test]
    public void NotFound()
    {
        // Act
        IResult result = Result.NotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void MapFailure_WhenSuccess_ShouldThrowException()
    {
        // Arrange
        IResult testResult = Result.Success();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => testResult.MapFailure<int>());
    }

    [Test]
    public void MapFailure_WhenNotSuccess_ShouldReturnResult()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        IResult testResult = Result.InternalError(testErrorMessage);

        // Act
        IResult<int> result = testResult.MapFailure<int>();

        AssertInternalError(result, testErrorMessage);
    }
}
