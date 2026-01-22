using DannyGoodacre.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DannyGoodacre.Identity.Tests.Extensions;

[TestFixture]
public class ResultExtensionsTests : TestBase
{
    [Test]
    public void ToResult_WhenIdentitySucceeded_ShouldReturnSuccess()
    {
        // Arrange
        var identityResult = IdentityResult.Success;

        // Act
        var result = identityResult.ToResult();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public void ToResult_WhenIdentityFailed_ShouldReturnInternalError()
    {
        // Arrange
        var identityResult = IdentityResult.Failed(new IdentityError
        {
            Code = "Test Code",
            Description = "Test Description"
        });

        // Act
        var result = identityResult.ToResult();

        // Assert
        AssertInternalError(result, "Failed : Test Code");
    }

    [Test]
    public void ToResult_WhenSignInSucceeded_ShouldReturnSuccess()
    {
        // Arrange
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.Success;

        // Act
        var result = signInResult.ToResult();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public void ToResult_WhenSignInFailed_ShouldReturnInternalError()
    {
        // Arrange
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.Failed;

        // Act
        var result = signInResult.ToResult();

        // Assert
        AssertInternalError(result, "Failed");
    }

    [Test]
    public void ToHttpResponse_WhenSuccess_ShouldReturnOk()
    {
        // Arrange
        var internalResult = Result.Success();

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.EqualTo(Results.Ok()));
    }

    [Test]
    public void ToHttpResponse_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var validationState = new ValidationState();

        validationState.AddError("Property 1", "Message 1");

        var internalResult = Result.Invalid(validationState);

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.TypeOf<BadRequest<ValidationProblemDetails>>());
    }

    [Test]
    public void ToHttpResponse_WhenDomainError_ShouldReturnBadRequest()
    {
        // Arrange
        var internalResult = Result.DomainError("Test Domain Error");

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.TypeOf<BadRequest<string>>());
    }

    [Test]
    public void ToHttpResponse_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var internalResult = Result.NotFound();

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.EqualTo(Results.NotFound()));
    }

    [Test]
    public void ToHttpResponse_WhenCancelled_ShouldReturnBadRequest()
    {
        // Arrange
        var internalResult = Result.Cancelled();

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.TypeOf<BadRequest<string>>());

        var badResult = result as BadRequest<string>;

        Assert.That(badResult?.Value, Is.EqualTo("The request was cancelled."));
    }

    [Test]
    public void ToHttpResponse_WhenInternalError_ShouldReturnInternalServerError()
    {
        // Arrange
        var internalResult = Result.InternalError("Test Internal Error");

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.EqualTo(Results.InternalServerError()));
    }
}
