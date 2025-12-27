using DannyGoodacre.Core.Common;
using DannyGoodacre.Tests.Common;
using NUnit.Framework;

namespace DannyGoodacre.Core.Tests.Common;

[TestFixture]
public class ResultTests : TestBase
{
    [Test]
    public void Success()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.Success));
    }

    [Test]
    public void FailedWithMessage()
    {
        // Arrange
        const string message = "Test Message";

        // Act
        var result = Result.InternalError(message);

        // Assert
        using (Assert.EnterMultipleScope())
        {

            Assert.That(result.Status, Is.EqualTo(Status.InternalError));
            Assert.That(result.Error, Is.EqualTo(message));
        }
    }

    [Test]
    public void FailedWithException()
    {
        // Arrange
        var exception = new Exception("Test Exception");

        // Act
        var result = Result.InternalError(exception);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.InternalError));
            Assert.That(result.Exception, Is.EqualTo(exception));
        }
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
        var result = Result.Invalid(validationState);

        // Assert
        using (Assert.EnterMultipleScope())
        {

            Assert.That(result.Status, Is.EqualTo(Status.Invalid));
            Assert.That(result.ValidationState, Is.EqualTo(validationState));
        }
    }

    [Test]
    public void NotFound()
    {
        // Act
        var result = Result.NotFound();

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.NotFound));
    }

    [Test]
    public void Cancelled()
    {
        // Act
        var result = Result.Cancelled();

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.Cancelled));
    }

    [Test]
    public void InternalError()
    {
        // Act
        var result = Result.InternalError("Test Error");

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.InternalError));
    }

    [Test]
    public void InternalErrorWithMessage()
    {
        // Arrange
        const string message = "Test Message";

        // Act
        var result = Result.InternalError(message);

        // Assert
        using (Assert.EnterMultipleScope())
        {

            Assert.That(result.Status, Is.EqualTo(Status.InternalError));
            Assert.That(result.Error, Is.EqualTo(message));
        }
    }

    [Test]
    public void IsSuccess_WhenSuccessful_ReturnsTrue()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void IsSuccess_WhenUnsuccessful_ReturnsFalse()
    {
        // Act
        var result = Result.InternalError("Test Error");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void IsSuccessWithValue_WhenSuccessful_ReturnsTrue()
    {
        // Act
        var result = Result<int>.Success(123);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void IsSuccessWithValue_WhenUnsuccessful_ReturnsFalse()
    {
        // Act
        var result = Result<int>.InternalError("Test Error");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void Success_WithValue()
    {
        // Arrange
        const int value = 123;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.Success));
            Assert.That(result.Value, Is.EqualTo(value));
        }
    }

    [Test]
    public void Invalid_WithValue()
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
        var result = Result<int>.Invalid(validationState);

        // Assert
        using (Assert.EnterMultipleScope())
        {

            Assert.That(result.Status, Is.EqualTo(Status.Invalid));
            Assert.That(result.ValidationState, Is.EqualTo(validationState));
        }
    }

    [Test]
    public void NotFound_WithValue()
    {
        // Act
        var result = Result<int>.NotFound();

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.NotFound));
    }

    [Test]
    public void Cancelled_WithValue()
    {
        // Act
        var result = Result<int>.Cancelled();

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.Cancelled));
    }

    [Test]
    public void InternalError_WithValue()
    {
        // Act
        var result = Result<int>.InternalError("Test Error");

        // Assert
        Assert.That(result.Status, Is.EqualTo(Status.InternalError));
    }

    [Test]
    public void InternalErrorWithMessage_WithValue()
    {
        // Arrange
        const string message = "Test Message";

        // Act
        var result = Result<int>.InternalError(message);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.InternalError));
            Assert.That(result.Error, Is.EqualTo(message));
        }
    }
}
