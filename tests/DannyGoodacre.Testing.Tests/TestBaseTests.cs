using DannyGoodacre.Primitives;
using NUnit.Framework;

namespace DannyGoodacre.Testing.Tests;

[TestFixture]
public sealed class TestBaseTests
{
    private sealed record TestModel(int Id, string Name, List<string> Items);

    private sealed class TestBase : Testing.TestBase
    {
        public static void TestAssertSuccess(Result result) => AssertSuccess(result);

        public static void TestAssertSuccess<T>(Result<T> result, T expectedValue) => AssertSuccess(result, expectedValue);

        public static void TestAssertInvalid(Result result) => AssertInvalid(result);

        public static void TestAssertDomainError(Result result, string error) => AssertDomainError(result, error);

        public static void TestAssertConflict(Result result, string error) => AssertConflict(result, error);

        public static void TestAssertCanceled(Result result) => AssertCanceled(result);

        public static void TestAssertNotFound(Result result) => AssertNotFound(result);

        public static void TestAssertInternalError(Result result, string error) => AssertInternalError(result, error);

        public static void TestAssertInternalError(Result result, Exception exception) => AssertInternalError(result, exception);
    }

    [Test]
    public void AssertSuccess_WhenSuccess_DoesNotThrow()
    {
        // Arrange
        Result result = Result.Success();

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WhenNotSuccess_DoesThrow()
    {
        // Arrange
        Result result = Result.InternalError("Test Error Message");

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccess_DoesNotThrow()
    {
        // Arrange
        Result<int> result = Result<int>.Success(123);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccessAndValuesMatch_DoesNotThrow()
    {
        // Arrange
        TestModel expected = new(123, "Test Name", ["Test Item 1", "Test Item 2"]);

        TestModel actual = new(123, "Test Name", ["Test Item 1", "Test Item 2"]);

        Result<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenNotSuccess_DoesThrow()
    {
        // Arrange
        Result<int> result = Result<int>.InternalError("Test Error Message");

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenNotSuccessAndExpectedValueProvided_DoesThrow()
    {
        // Arrange
        TestModel expected = new(1, "Test", ["A"]);

        Result<TestModel> result = Result<TestModel>.InternalError("Test Error Message");

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccessAndValuesDoNotMatch_DoesThrow()
    {
        // Arrange
        TestModel expected = new(1, "Test Expected Name", ["Test Item 1"]);

        TestModel actual = new(1, "Test Actual Name", ["Test Item 1"]);

        Result<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccessAndNestedPropertiesDoNotMatch_DoesThrow()
    {
        // Arrange
        TestModel expected = new(1, "Test Name", ["Test Expected Item 1"]);

        TestModel actual = new(1, "Test Name", ["Test Actual Item 1"]);

        Result<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertInvalid_WhenInvalid_DoesNotThrow()
    {
        // Arrange
        ValidationState state = new();

        state.AddError("Test Property", "Test Error");

        Result result = Result.Invalid(state);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertInvalid(result));
    }

    [Test]
    public void AssertInvalid_WhenNotInvalid_DoesThrow()
    {
        // Arrange
        ValidationState state = new();

        state.AddError("Test Property", "Test Error");

        Result result = Result.Success();

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertInvalid(result));
    }

    [Test]
    public void AssertDomainError_WhenDomainError_DoesNotThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        Result result = Result.DomainError(message);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertDomainError(result, message));
    }

    [Test]
    public void AssertDomainError_WhenDomainErrorAndMessagesDoNotMatch_DoesThrow()
    {
        // Arrange
        const string expectedMessage = "Test Expected Message";

        const string actualMessage = "Test Actual Message";

        Result result = Result.DomainError(actualMessage);

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertDomainError(result, expectedMessage));
    }

    [Test]
    public void AssertDomainError_WhenNotDomainError_DoesThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        Result result = Result.Success();

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertDomainError(result, message));
    }

    [Test]
    public void AssertConflict_WhenConflict_DoesNotThrow()
    {
        // Arrange
        const string message = "Test Conflict Message";

        Result result = Result.Conflict(message);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertConflict(result, message));
    }

    [Test]
    public void AssertConflict_WhenConflictAndMessagesDoNotMatch_DoesThrow()
    {
        // Arrange
        const string expectedMessage = "Test Expected Message";

        const string actualMessage = "Test Actual Message";

        Result result = Result.Conflict(actualMessage);

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertConflict(result, expectedMessage));
    }

    [Test]
    public void AssertCanceled_WhenCanceled_DoesNotThrow()
    {
        // Arrange
        Result result = Result.Canceled();

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertCanceled(result));
    }

    [Test]
    public void AssertCanceled_WhenNotCanceled_DoesThrow()
    {
        // Arrange
        Result result = Result.Success();

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertCanceled(result));
    }

    [Test]
    public void AssertNotFound_WhenNotFound_DoesNotThrow()
    {
        // Arrange
        Result result = Result.NotFound();

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertNotFound(result));
    }

    [Test]
    public void AssertNotFound_WhenNotNotFound_DoesNotThrow()
    {
        // Arrange
        Result result = Result.Success();

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertNotFound(result));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithMessage_DoesNotThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        Result result = Result.InternalError(message);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertInternalError(result, message));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithMessagesAndMessagesDoNotMatch_DoesThrow()
    {
        // Arrange
        const string expectedMessage = "Test Expected Message";

        const string actualMessage = "Test Actual Message";

        Result result = Result.InternalError(actualMessage);

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertInternalError(result, expectedMessage));
    }

    [Test]
    public void AssertInternalError_WhenNotInternalError_DoesThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        Result result = Result.Success();

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertInternalError(result, message));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithException_ShouldNotThrowException()
    {
        // Arrange
        Exception exception = new("Test Exception");

        Result result = Result.InternalError(exception);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertInternalError(result, exception));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithExceptionAndExceptionsDoNotMatch_ShouldNotThrowException()
    {
        // Arrange
        Exception expectedException = new("Test Expected Exception");

        Exception actualException = new("Test Actual Exception");

        Result result = Result.InternalError(actualException);

        // Act & Assert
        Assert.Throws<MultipleAssertException>(() => TestBase.TestAssertInternalError(result, expectedException));
    }
}
