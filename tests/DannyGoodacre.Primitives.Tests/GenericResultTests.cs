using System.Diagnostics.CodeAnalysis;

namespace DannyGoodacre.Primitives.Tests;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
public sealed class GenericResultTests
{
    [Fact]
    public void Success_CreatesGenericSuccessAndReturnsValue()
    {
        // Arrange
        const int expectedValue = 42;

        // Act
        IResult<int> result = Result<int>.Success(expectedValue);

        // Assert
        var success = Assert.IsType<Success<int>>(result);

        Assert.Equal(expectedValue, success.Value);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Canceled_CreatesGenericCanceledAndInheritsNonGenericCanceled()
    {
        // Act
        IResult<string> result = Result<string>.Canceled();

        // Assert
        Assert.IsType<Canceled<string>>(result);
        Assert.IsType<Canceled>(result, exactMatch: false);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Conflict_CreatesGenericConflictAndInheritsNonGenericConflict()
    {
        // Arrange
        const string expectedMessage = "Item exists";

        // Act
        IResult<string> result = Result<string>.Conflict(expectedMessage);

        // Assert
        var conflict = Assert.IsType<Conflict<string>>(result);

        Assert.IsType<Conflict>(result, exactMatch: false);
        Assert.Equal(expectedMessage, conflict.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void DomainError_CreatesGenericDomainErrorAndInheritsNonGenericDomainError()
    {
        // Arrange
        const string expectedMessage = "Domain constraint violated";

        // Act
        IResult<string> result = Result<string>.DomainError(expectedMessage);

        // Assert
        var domainError = Assert.IsType<DomainError<string>>(result);

        Assert.IsType<DomainError>(result, exactMatch: false);
        Assert.Equal(expectedMessage, domainError.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void NotFound_CreatesGenericNotFoundAndInheritsNonGenericNotFound()
    {
        // Act
        IResult<string> result = Result<string>.NotFound();

        // Assert
        Assert.IsType<NotFound<string>>(result);
        Assert.IsType<NotFound>(result, exactMatch: false);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromErrorStruct_CreatesGenericInternalError()
    {
        // Arrange
        var error = new Error("Database timeout");

        // Act
        IResult<string> result = Result<string>.InternalError(error);

        // Assert
        var internalError = Assert.IsType<InternalError<string>>(result);

        Assert.IsType<InternalError>(result, exactMatch: false);
        Assert.Equal(error, internalError.Error);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromStringMessage_CreatesGenericInternalError()
    {
        // Arrange
        const string expectedMessage = "Internal processing failure";

        // Act
        IResult<string> result = Result<string>.InternalError(expectedMessage);

        // Assert
        var internalError = Assert.IsType<InternalError<string>>(result);

        Assert.IsType<InternalError>(result, exactMatch: false);
        Assert.Equal(expectedMessage, internalError.Error.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromException_CreatesGenericInternalErrorWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Unhandled failure");

        // Act
        IResult<string> result = Result<string>.InternalError(exception);

        // Assert
        var internalError = Assert.IsType<InternalError<string>>(result);

        Assert.IsType<InternalError>(result, exactMatch: false);
        Assert.Equal(exception, internalError.Error.Exception);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Invalid_CreatesGenericInvalidAndInheritsNonGenericInvalid()
    {
        // Arrange
        var validationState = new ValidationState();

        // Act
        IResult<string> result = Result<string>.Invalid(validationState);

        // Assert
        var invalid = Assert.IsType<Invalid<string>>(result);

        Assert.IsType<Invalid>(result, exactMatch: false);
        Assert.Same(validationState, invalid.ValidationState);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void DomainError_ToString_UsesTheSameFormatAsNonGeneric()
    {
        // Act
        IResult<string> result = Result<string>.DomainError("Invalid business rule");

        // Assert
        Assert.Equal("DomainError: Invalid business rule", result.ToString());
    }

    [Fact]
    public void InternalError_ToString_UsesTheSameFormatAsNonGeneric()
    {
        // Arrange
        var error = new Error("Database error");

        // Act
        IResult<string> result = Result<string>.InternalError(error);

        // Assert
        Assert.Equal($"InternalError: {error}", result.ToString());
    }

    [Fact]
    public void Factories_ReturnInterfaces_SoIsSuccessIsAvailableWithVar()
    {
        // Act
        var success = Result<int>.Success(1);
        var failure = Result<int>.NotFound();

        // Assert
        Assert.True(success.IsSuccess);
        Assert.True(failure.IsFailure);
    }
}
