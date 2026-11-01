using System.Diagnostics.CodeAnalysis;

namespace DannyGoodacre.Primitives.Tests;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
public sealed class ResultFactoryTests
{
    [Fact]
    public void Success_CreatesSuccessInstance()
    {
        // Act
        IResult result = Result.Success();

        // Assert
        Assert.IsType<Success>(result);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void SuccessGeneric_CreatesGenericSuccessWithValue()
    {
        // Arrange
        const string expectedValue = "Success Payload";

        // Act
        IResult<string> result = Result.Success(expectedValue);

        // Assert
        var success = Assert.IsType<Success<string>>(result);

        Assert.Equal(expectedValue, success.Value);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Canceled_CreatesCanceledInstance()
    {
        // Act
        IResult result = Result.Canceled();

        // Assert
        Assert.IsType<Canceled>(result);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Conflict_CreatesConflictInstanceWithMessage()
    {
        // Arrange
        const string expectedMessage = "Conflict occurred";

        // Act
        IResult result = Result.Conflict(expectedMessage);

        // Assert
        var conflict = Assert.IsType<Conflict>(result);

        Assert.Equal(expectedMessage, conflict.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void DomainError_CreatesDomainErrorInstanceWithMessage()
    {
        // Arrange
        const string expectedMessage = "Domain rule failed";

        // Act
        IResult result = Result.DomainError(expectedMessage);

        // Assert
        var domainError = Assert.IsType<DomainError>(result);

        Assert.Equal(expectedMessage, domainError.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromErrorStruct_CreatesInternalErrorInstance()
    {
        // Arrange
        var error = new Error("Internal error description");

        // Act
        IResult result = Result.InternalError(error);

        // Assert
        var internalError = Assert.IsType<InternalError>(result);

        Assert.Equal(error, internalError.Error);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromStringMessage_CreatesInternalErrorInstanceWithError()
    {
        // Arrange
        const string expectedMessage = "Error message text";

        // Act
        IResult result = Result.InternalError(expectedMessage);

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
        var exception = new InvalidOperationException("Unhandled exception");

        // Act
        IResult result = Result.InternalError(exception);

        // Assert
        var internalError = Assert.IsType<InternalError>(result);

        Assert.Same(exception, internalError.Error.Exception);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Invalid_CreatesInvalidInstanceWithValidationState()
    {
        // Arrange
        var validationState = new ValidationState();

        // Act
        IResult result = Result.Invalid(validationState);

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
        IResult result = Result.NotFound();

        // Assert
        Assert.IsType<NotFound>(result);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }
}
