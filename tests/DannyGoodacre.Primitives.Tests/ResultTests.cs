using System.Diagnostics.CodeAnalysis;

namespace DannyGoodacre.Primitives.Tests;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
public sealed class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessInstanceAndSetsIsSuccessTrue()
    {
        // Act
        IResult result = new Success();

        // Assert
        Assert.IsType<Success>(result);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void SuccessGeneric_CreatesGenericSuccessInstanceAndSetsIsSuccessTrue()
    {
        // Act
        IResult<string> result = new Success<string>("Payload");

        // Assert
        var success = Assert.IsType<Success<string>>(result);

        Assert.Equal("Payload", success.Value);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Canceled_CreatesCanceledInstanceAndSetsIsFailureTrue()
    {
        // Act
        IResult result = new Canceled();

        // Assert
        Assert.IsType<Canceled>(result);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Conflict_CreatesConflictInstanceWithMessage()
    {
        // Arrange
        const string expectedMessage = "Resource conflict detected";

        // Act
        IResult result = new Conflict(expectedMessage);

        // Assert
        var conflict = Assert.IsType<Conflict>(result);

        Assert.Equal(expectedMessage, conflict.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void DomainError_CreatesDomainErrorInstanceWithMessageAndFormatsToString()
    {
        // Arrange
        const string expectedMessage = "Invalid business rule";

        // Act
        IResult result = new DomainError(expectedMessage);

        // Assert
        var domainError = Assert.IsType<DomainError>(result);

        Assert.Equal(expectedMessage, domainError.Message);
        Assert.Equal("DomainError: Invalid business rule", domainError.ToString());
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromErrorStruct_CreatesInternalErrorInstance()
    {
        // Arrange
        var error = new Error("Database error");

        // Act
        IResult result = new InternalError(error);

        // Assert
        var internalError = Assert.IsType<InternalError>(result);

        Assert.Equal(error, internalError.Error);
        Assert.Equal($"InternalError: {error}", internalError.ToString());
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromStringMessage_CreatesInternalErrorInstance()
    {
        // Arrange
        const string expectedMessage = "Unexpected failure";

        // Act
        IResult result = new InternalError(expectedMessage);

        // Assert
        var internalError = Assert.IsType<InternalError>(result);

        Assert.Equal(expectedMessage, internalError.Error.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromException_CreatesInternalErrorInstanceWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Fatal error");

        // Act
        IResult result = new InternalError(exception);

        // Assert
        var internalError = Assert.IsType<InternalError>(result);

        Assert.Equal(exception, internalError.Error.Exception);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Invalid_CreatesInvalidInstanceWithValidationState()
    {
        // Arrange
        var validationState = new ValidationState();

        validationState.AddError("Field", "Required");

        // Act
        IResult result = new Invalid(validationState);

        // Assert
        var invalid = Assert.IsType<Invalid>(result);

        Assert.Same(validationState, invalid.ValidationState);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void NotFound_CreatesNotFoundInstance()
    {
        // Act
        IResult result = new NotFound();

        // Assert
        Assert.IsType<NotFound>(result);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Factories_ReturnInterfaces_SoIsSuccessIsAvailableWithVar()
    {
        // Act
        var success = Result.Success();
        var failure = Result.DomainError("Invalid business rule");

        // Assert
        Assert.True(success.IsSuccess);
        Assert.True(failure.IsFailure);
    }
}
