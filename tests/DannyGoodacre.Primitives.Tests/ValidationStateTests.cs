namespace DannyGoodacre.Primitives.Tests;

[TestFixture]
public sealed class ValidationStateTests : TestBase
{
    [Test]
    public void Constructor_WhenErrorsIsEmpty_ShouldReturnNoErrors()
    {
        // Act
        var validationState = new ValidationState();

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
        const string testProperty = "Test Property";

        const string testError1 = "Test Error 1";
        const string testError2 = "Test Error 2";

        var validationState = new ValidationState();

        // Act
        validationState.AddError(testProperty, testError1);
        validationState.AddError(testProperty, testError2);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(validationState.HasErrors, Is.True);

            Assert.That(validationState.Errors, Has.Count.EqualTo(1));

            Assert.That(validationState.Errors[testProperty], Is.EquivalentTo([testError1, testError2]));
        }
    }

    [Test]
    public void AddError_WhenErrorsWithDifferentProperties_ShouldReturnErrors()
    {
        // Arrange
        const string testProperty1 = "Test Property 1";
        const string testError1 = "Test Error 1";

        const string testProperty2 = "Test Property 2";
        const string testError2 = "Test Error 2";

        var validationState = new ValidationState();

        // Act
        validationState.AddError(testProperty1, testError1);
        validationState.AddError(testProperty2, testError2);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(validationState.HasErrors, Is.True);
            Assert.That(validationState.Errors, Has.Count.EqualTo(2));

            Assert.That(validationState.Errors[testProperty1], Is.EqualTo([testError1]));
            Assert.That(validationState.Errors[testProperty2], Is.EqualTo([testError2]));
        }
    }

    [Test]
    public void ToString_WhenErrorsIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var validationState = new ValidationState();

        // Act
        string error = validationState.ToString();

        // Assert
        Assert.That(error, Is.Empty);
    }

    [Test]
    public void ToString_WhenErrors_ShouldReturnErrorsAsString()
    {
        // Arrange
        const string testProperty1 = "Test Property 1";
        const string testError1 = "Test Error 1";

        const string testProperty2 = "Test Property 2";
        const string testError2 = "Test Error 2";

        var validationState = new ValidationState();

        validationState.AddError(testProperty1, testError1);
        validationState.AddError(testProperty2, testError2);

        string expectedError = $"{testProperty1}:{Environment.NewLine}  - {testError1}{Environment.NewLine}{testProperty2}:{Environment.NewLine}  - {testError2}";

        // Act
        string error = validationState.ToString();

        // Assert
        Assert.That(error, Is.EqualTo(expectedError));
    }
}
