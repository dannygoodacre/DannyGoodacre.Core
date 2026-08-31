using DannyGoodacre.Testing;
using NUnit.Framework;

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
    public void Invalid()
    {
        // Arrange
        ValidationState validationState = new ValidationState();

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
    public void ConflictWithValue()
    {
        // Arrange
        const string message = "Test Conflict Message";

        // Act
        IResult<int> result = Result.Conflict<int>(message);

        // Assert
        AssertConflict(result, message);
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
    public void NotFound()
    {
        // Act
        IResult result = Result.NotFound();

        // Assert
        AssertNotFound(result);
    }

    [Test]
    public void InternalError_WithMessage()
    {
        // Act
        const string message = "Test Error Message";

        NUnit.Framework.Result result = NUnit.Framework.Result.InternalError(message);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.InternalError));

            Assert.That(result.Error, Is.EqualTo(message));

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void InternalError_WithException()
    {
        // Arrange
        Exception exception = new Exception("Test Exception");

        // Act
        NUnit.Framework.Result result = NUnit.Framework.Result.InternalError(exception);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.InternalError));

            Assert.That(result.Error, Is.Null);

            Assert.That(result.Exception, Is.EqualTo(exception).UsingPropertiesComparer());

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void Success_WithImplicitValue()
    {
        // Arrange
        const int value = 123;

        // Act
        Result<int> result = NUnit.Framework.Result.Success(value);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.Success));

            Assert.That(result.Value, Is.EqualTo(value));

            Assert.That(result.Error, Is.Null);

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void MapFailure_WhenSuccess_ShouldThrowException()
    {
        // Arrange
        NUnit.Framework.Result testResult = NUnit.Framework.Result.Success();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => testResult.MapFailure<int>());
    }

    [Test]
    public void MapFailure_WhenSuccessWithValue_ShouldThrowException()
    {
        // Arrange
        const int testValue = 123;

        Result<int> testResult = Result<int>.Success(testValue);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => testResult.MapFailure<string>());
    }

    [Test]
    public void MapFailure_WhenNotSuccess_ShouldReturnResult()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        NUnit.Framework.Result testResult = NUnit.Framework.Result.InternalError(testErrorMessage);

        // Act
        Result<int> result = testResult.MapFailure<int>();

        AssertInternalError(result, testErrorMessage);
    }

    [Test]
    public void MapFailure_WhenNotSuccessWithValue_ShouldReturnResult()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        Result<int> testResult = Result<int>.InternalError(testErrorMessage);

        // Act
        Result<string> result = testResult.MapFailure<string>();

        AssertInternalError(result, testErrorMessage);
    }
}
