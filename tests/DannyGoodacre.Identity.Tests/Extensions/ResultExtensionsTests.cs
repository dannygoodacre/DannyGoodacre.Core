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
    public void ToResult_WhenIdentityFailed_ShouldReturnDomainError()
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
        AssertDomainError(result, "Failed : Test Code");
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
    public void ToResult_WhenSignInFailed_ShouldReturnDomainError()
    {
        // Arrange
        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.Failed;

        // Act
        var result = signInResult.ToResult();

        // Assert
        AssertDomainError(result, "Failed");
    }

    [Test]
    public void ToHttpResponse_WhenSuccess_ShouldReturnNoContent()
    {
        // Arrange
        var internalResult = Result.Success();

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.EqualTo(Results.NoContent()));
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
    public void ToHttpResponse_WhenDomainError_ShouldReturnProblemDetails400()
    {
        // Arrange
        const string errorMessage = "Test Domain Error";

        var internalResult = Result.Failed(errorMessage);

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.TypeOf<ProblemHttpResult>());

        var problemResult = result as ProblemHttpResult;

        Assert.That(problemResult?.StatusCode, Is.EqualTo(400));

        Assert.That(problemResult?.ProblemDetails.Detail, Is.EqualTo(errorMessage));
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
    public void ToHttpResponse_WhenCancelled_ShouldReturnStatus499()
    {
        // Arrange
        var internalResult = Result.Cancelled();

        // Act
        var result = internalResult.ToHttpResponse();

        // Assert
        Assert.That(result, Is.TypeOf<StatusCodeHttpResult>());

        var statusCodeResult = result as IStatusCodeHttpResult;

        Assert.That(statusCodeResult?.StatusCode, Is.EqualTo(499));
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
