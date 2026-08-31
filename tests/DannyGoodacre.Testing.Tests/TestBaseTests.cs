using DannyGoodacre.Primitives;
using NUnit.Framework;

namespace DannyGoodacre.Testing.Tests;

[TestFixture]
public sealed class TestBaseTests
{
    private sealed record TestModel(int Id, string Name, List<string> Items);

    private sealed class TestBase : Testing.TestBase
    {
        public static void TestAssertSuccess(IResult result) => AssertSuccess(result);

        public static void TestAssertSuccess<T>(IResult<T> result, T expectedValue) => AssertSuccess(result, expectedValue);

        public static void TestAssertInvalid(IResult result, ValidationState validationState) => AssertInvalid(result, validationState);

        public static void TestAssertDomainError(IResult result, string error) => AssertDomainError(result, error);

        public static void TestAssertConflict(IResult result, string error) => AssertConflict(result, error);

        public static void TestAssertCanceled(IResult result) => AssertCanceled(result);

        public static void TestAssertNotFound(IResult result) => AssertNotFound(result);

        public static void TestAssertInternalError(IResult result, string error) => AssertInternalError(result, error);

        public static void TestAssertInternalError(IResult result, Exception exception) => AssertInternalError(result, exception);
    }

    [Test]
    public void AssertSuccess_WhenSuccess_DoesNotThrow()
    {
        // Arrange
        IResult result = new Success();

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WhenNotSuccess_DoesThrow()
    {
        // Arrange
        IResult result = new InternalError("Test Error Message");

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccess_DoesNotThrow()
    {
        // Arrange
        IResult<int> result = new Success<int>(123);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccessAndValuesMatch_DoesNotThrow()
    {
        // Arrange
        TestModel expected = new(123, "Test Name", ["Test Item 1", "Test Item 2"]);

        TestModel actual = new(123, "Test Name", ["Test Item 1", "Test Item 2"]);

        IResult<TestModel> result = new Success<TestModel>(actual);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenNotSuccess_DoesThrow()
    {
        // Arrange
        IResult<int> result = new InternalError<int>("Test Error Message");

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertSuccess(result));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenNotSuccessAndExpectedValueProvided_DoesThrow()
    {
        // Arrange
        var expected = new TestModel(1, "Test", ["A"]);

        IResult<TestModel> result = new InternalError<TestModel>("Test Error Message");

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccessAndValuesDoNotMatch_DoesThrow()
    {
        // Arrange
        TestModel expected = new(1, "Test Expected Name", ["Test Item 1"]);

        TestModel actual = new(1, "Test Actual Name", ["Test Item 1"]);

        IResult<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WithValue_WhenSuccessAndNestedPropertiesDoNotMatch_DoesThrow()
    {
        // Arrange
        TestModel expected = new(1, "Test Name", ["Test Expected Item 1"]);

        TestModel actual = new(1, "Test Name", ["Test Actual Item 1"]);

        IResult<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertInvalid_WhenInvalid_DoesNotThrow()
    {
        // Arrange
        ValidationState state = new();

        state.AddError("Test Property", "Test Error");

        IResult result = Result.Invalid(state);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertInvalid(result, state));
    }

    [Test]
    public void AssertInvalid_WhenNotInvalid_DoesThrow()
    {
        // Arrange
        ValidationState state = new();

        state.AddError("Test Property", "Test Error");

        IResult result = Result.Success();

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertInvalid(result, state));
    }

    [Test]
    public void AssertDomainError_WhenDomainError_DoesNotThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        IResult result = Result.DomainError(message);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertDomainError(result, message));
    }

    [Test]
    public void AssertDomainError_WhenDomainErrorAndMessagesDoNotMatch_DoesThrow()
    {
        // Arrange
        const string expectedMessage = "Test Expected Message";

        const string actualMessage = "Test Actual Message";

        IResult result = Result.DomainError(actualMessage);

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertDomainError(result, expectedMessage));
    }

    [Test]
    public void AssertDomainError_WhenNotDomainError_DoesThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        IResult result = Result.Success();

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertDomainError(result, message));
    }

    [Test]
    public void AssertConflict_WhenConflict_DoesNotThrow()
    {
        // Arrange
        const string message = "Test Conflict Message";

        IResult result = Result.Conflict(message);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertConflict(result, message));
    }

    [Test]
    public void AssertConflict_WhenConflictAndMessagesDoNotMatch_DoesThrow()
    {
        // Arrange
        const string expectedMessage = "Test Expected Message";

        const string actualMessage = "Test Actual Message";

        IResult result = Result.Conflict(actualMessage);

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertConflict(result, expectedMessage));
    }

    [Test]
    public void AssertCanceled_WhenCanceled_DoesNotThrow()
    {
        // Arrange
        IResult result = Result.Canceled();

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertCanceled(result));
    }

    [Test]
    public void AssertCanceled_WhenNotCanceled_DoesThrow()
    {
        // Arrange
        IResult result = Result.Success();

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertCanceled(result));
    }

    [Test]
    public void AssertNotFound_WhenNotFound_DoesNotThrow()
    {
        // Arrange
        IResult result = Result.NotFound();

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertNotFound(result));
    }

    [Test]
    public void AssertNotFound_WhenNotNotFound_DoesNotThrow()
    {
        // Arrange
        IResult result = Result.Success();

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertNotFound(result));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithMessage_DoesNotThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        IResult result = Result.InternalError(message);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertInternalError(result, message));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithMessagesAndMessagesDoNotMatch_DoesThrow()
    {
        // Arrange
        const string expectedMessage = "Test Expected Message";

        const string actualMessage = "Test Actual Message";

        IResult result = Result.InternalError(actualMessage);

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertInternalError(result, expectedMessage));
    }

    [Test]
    public void AssertInternalError_WhenNotInternalError_DoesThrow()
    {
        // Arrange
        const string message = "Test Error Message";

        IResult result = Result.Success();

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertInternalError(result, message));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithException_ShouldNotThrowException()
    {
        // Arrange
        Exception exception = new("Test Exception");

        IResult result = Result.InternalError(exception);

        // Act & Assert
        Assert.DoesNotThrow(() => TestBase.TestAssertInternalError(result, exception));
    }

    [Test]
    public void AssertInternalError_WhenInternalErrorWithExceptionAndExceptionsDoNotMatch_ShouldNotThrowException()
    {
        // Arrange
        Exception expectedException = new("Test Expected Exception");

        Exception actualException = new("Test Actual Exception");

        IResult result = Result.InternalError(actualException);

        // Act & Assert
        Assert.Throws<AssertionException>(() => TestBase.TestAssertInternalError(result, expectedException));
    }
}
