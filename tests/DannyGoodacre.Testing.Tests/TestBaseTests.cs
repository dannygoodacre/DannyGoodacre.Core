using DannyGoodacre.Primitives;
using NUnit.Framework;

namespace DannyGoodacre.Testing.Tests;

[TestFixture]
public sealed class TestBaseTests
{
    private sealed record TestModel(int Id, string Name, List<string> Items);

    private sealed class TestWrapper : TestBase
    {
        public static void TestAssertSuccess<T>(Result<T> result, T expectedValue)
            => AssertSuccess(result, expectedValue);
    }

    [Test]
    public void AssertSuccess_WhenSuccessfulAndValuesMatch_ShouldNotThrowAssertionException()
    {
        // Arrange
        TestModel expected = new(123, "Test Name", ["Test Item 1", "Test Item 2"]);

        TestModel actual = new(123, "Test Name", ["Test Item 1", "Test Item 2"]);

        Result<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        Assert.DoesNotThrow(() => TestWrapper.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WhenNotSuccessful_ShouldThrowAssertionException()
    {
        // Arrange
        TestModel expected = new TestModel(1, "Test", ["A"]);

        Result<TestModel> result = Result<TestModel>.InternalError("Test Error Message");

        // Act & Assert
        MultipleAssertException? exception = Assert.Throws<MultipleAssertException>(()
            => TestWrapper.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WhenPropertiesMismatch_ShouldThrowAssertionException()
    {
        // Arrange
        TestModel expected = new TestModel(1, "Test Expected Name", ["Test Item 1"]);

        TestModel actual = new TestModel(1, "Test Actual Name", ["Test Item 1"]);

        Result<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        MultipleAssertException? exception = Assert.Throws<MultipleAssertException>(()
            => TestWrapper.TestAssertSuccess(result, expected));
    }

    [Test]
    public void AssertSuccess_WhenNestedPropertiesMismatch_ShouldThrowAssertionException()
    {
        // Arrange
        TestModel expected = new TestModel(1, "Test Name", ["Test Expected Item 1"]);

        TestModel actual = new TestModel(1, "Test Name", ["Test Actual Item 1"]);

        Result<TestModel> result = Result<TestModel>.Success(actual);

        // Act & Assert
        MultipleAssertException? exception = Assert.Throws<MultipleAssertException>(()
            => TestWrapper.TestAssertSuccess(result, expected));
    }
}
