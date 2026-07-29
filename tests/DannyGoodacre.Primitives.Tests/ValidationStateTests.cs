using DannyGoodacre.Testing;
using NUnit.Framework;

namespace DannyGoodacre.Primitives.Tests;

[TestFixture]
public sealed class ValidationStateTests : TestBase
{
    [Test]
    public void Constructor_WhenErrorsIsEmpty_ShouldReturnNoErrors()
    {
        // Act
        ValidationState validationState = new();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(validationState.Errors, Is.Empty);
            Assert.That(validationState.HasErrors, Is.False);
        }
    }

    [Test]
    public void AddError_WhenErrorsWithSameProperties_ShouldReturnErrors()
    {
        // Arrange
        const string property = "Test Property";

        const string error1 = "Test Error 1";

        const string error2 = "Test Error 2";

        ValidationState validationState = new();

        // Act
        validationState.AddError(property, error1);
        validationState.AddError(property, error2);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(validationState.HasErrors, Is.True);
            Assert.That(validationState.Errors, Has.Count.EqualTo(1));
            Assert.That(validationState.Errors[property], Is.EquivalentTo([error1, error2]));
        }
    }

    [Test]
    public void AddError_WhenErrorsWithDifferentProperties_ShouldReturnErrors()
    {
        // Arrange
        const string property1 = "Test Property 1";
        const string error1 = "Test Error 1";

        const string property2 = "Test Property 2";
        const string error2 = "Test Error 2";

        ValidationState validationState = new();

        // Act
        validationState.AddError(property1, error1);
        validationState.AddError(property2, error2);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(validationState.HasErrors, Is.True);
            Assert.That(validationState.Errors, Has.Count.EqualTo(2));

            Assert.That(validationState.Errors[property1], Is.EqualTo([error1]));
            Assert.That(validationState.Errors[property2], Is.EqualTo([error2]));
        }
    }

    [Test]
    public void ToString_WhenErrorsIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        ValidationState validationState = new();

        // Act
        string error = validationState.ToString();

        // Assert
        Assert.That(error, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToString_WhenErrors_ShouldReturnErrorsAsString()
    {
        // Arrange
        const string property1 = "Test Property 1";
        const string error1 = "Test Error 1";

        const string property2 = "Test Property 2";
        const string error2 = "Test Error 2";

        string expectedError = $"{property1}:{Environment.NewLine}  - {error1}{Environment.NewLine}{property2}:{Environment.NewLine}  - {error2}";

        ValidationState validationState = new();

        validationState.AddError(property1, error1);
        validationState.AddError(property2, error2);

        // Act
        string error = validationState.ToString();

        // Assert
        Assert.That(error, Is.EqualTo(expectedError));
    }
}
