using DannyGoodacre.Core;
using DannyGoodacre.Identity.Extensions;
using DannyGoodacre.Testing.Core;
using NUnit.Framework;

namespace DannyGoodacre.Identity.Tests.Extensions;

[TestFixture]
public class ValidationStateExtensionsTests : TestBase
{
    [Test]
    public void ToValidationProblemDetails_WhenNoErrors_ShouldReturnEmpty()
    {
        // Arrange
        var validationState = new ValidationState();

        // Act
        var result = validationState.ToValidationProblemDetails();

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void ToValidationProblemDetails_WhenErrors_ShouldReturnErrors()
    {
        // Arrange
        var validationState = new ValidationState();

        validationState.AddError("Property 1", "Message 1");
        validationState.AddError("Property 1", "Message 2");
        validationState.AddError("Property 2", "Message 3");

        // Act
        var result = validationState.ToValidationProblemDetails();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Errors, Has.Count.EqualTo(2));

            Assert.That(result.Errors["Property 1"], Has.Length.EqualTo(2));

            Assert.That(result.Errors["Property 1"][0], Is.EqualTo("Message 1"));
            Assert.That(result.Errors["Property 1"][1], Is.EqualTo("Message 2"));

            Assert.That(result.Errors["Property 2"], Has.Length.EqualTo(1));

            Assert.That(result.Errors["Property 2"][0], Is.EqualTo("Message 3"));
        }
    }
}
