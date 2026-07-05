using DannyGoodacre.Testing;
using NUnit.Framework;

namespace DannyGoodacre.Primitives.Tests;

[TestFixture]
public sealed class ResultTests : TestBase
{
    [Test]
    public void IsSuccess_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        Result result = Result.Success();

        // Act
        bool isSuccess = result.IsSuccess;

        // Assert
        Assert.That(isSuccess, Is.True);
    }

    [Test]
    public void IsSuccess_WhenUnsuccessful_ShouldReturnFalse()
    {
        // Act
        Result result = Result.InternalError("Test Error Message");

        // Act
        bool isSuccess = result.IsSuccess;

        // Assert
        Assert.That(isSuccess, Is.False);
    }

    [Test]
    public void Success()
    {
        // Act
        Result result = Result.Success();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.Success));

            Assert.That(result.Error, Is.Null);

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
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
        Result result = Result.Invalid(validationState);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.Invalid));

            Assert.That(result.Error, Is.Null);

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.EqualTo(validationState).UsingPropertiesComparer());
        }
    }

    [Test]
    public void DomainError()
    {
        // Arrange
        const string message = "Test Error Message";

        // Act
        Result result = Result.DomainError(message);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.DomainError));

            Assert.That(result.Error, Is.EqualTo(message));

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void Conflict()
    {
        // Arrange
        const string message = "Test Conflict Message";

        // Act
        Result result = Result.Conflict(message);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.Conflict));

            Assert.That(result.Error, Is.EqualTo(message));

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void Canceled()
    {
        // Act
        Result result = Result.Canceled();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.Canceled));

            Assert.That(result.Error, Is.Null);

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void NotFound()
    {
        // Act
        Result result = Result.NotFound();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(Status.NotFound));

            Assert.That(result.Error, Is.Null);

            Assert.That(result.Exception, Is.Null);

            Assert.That(result.ValidationState, Is.Null);
        }
    }

    [Test]
    public void InternalError_WithMessage()
    {
        // Act
        const string message = "Test Error Message";

        Result result = Result.InternalError(message);

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
        Result result = Result.InternalError(exception);

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
        Result<int> result = Result.Success(value);

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
}
