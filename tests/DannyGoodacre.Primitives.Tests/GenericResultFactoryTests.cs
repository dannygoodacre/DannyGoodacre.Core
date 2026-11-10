using System.Diagnostics.CodeAnalysis;

namespace DannyGoodacre.Primitives.Tests;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
public sealed class GenericResultFactoryTests
{
    [Fact]
    public void Success_CreatesGenericSuccessWithValue()
    {
        // Arrange
        const int expectedValue = 100;

        // Act
        IResult<int> result = Result<int>.Success(expectedValue);

        // Assert
        var success = Assert.IsType<Success<int>>(result);

        Assert.Equal(expectedValue, success.Value);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Canceled_CreatesGenericCanceledInstance()
    {
        // Act
        IResult<string> result = Result<string>.Canceled();

        // Assert
        Assert.IsType<Canceled<string>>(result);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Conflict_CreatesGenericConflictInstanceWithMessage()
    {
        // Arrange
        const string expectedMessage = "Generic conflict";

        // Act
        IResult<string> result = Result<string>.Conflict(expectedMessage);

        // Assert
        var conflict = Assert.IsType<Conflict<string>>(result);

        Assert.Equal(expectedMessage, conflict.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void DomainError_CreatesGenericDomainErrorInstanceWithMessage()
    {
        // Arrange
        const string expectedMessage = "Generic domain error";

        // Act
        IResult<string> result = Result<string>.DomainError(expectedMessage);

        // Assert
        var domainError = Assert.IsType<DomainError<string>>(result);

        Assert.Equal(expectedMessage, domainError.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void NotFound_CreatesGenericNotFoundInstance()
    {
        // Act
        IResult<string> result = Result<string>.NotFound();

        // Assert
        Assert.IsType<NotFound<string>>(result);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromErrorStruct_CreatesGenericInternalErrorInstance()
    {
        // Arrange
        var error = new Error("Generic error payload");

        // Act
        IResult<string> result = Result<string>.InternalError(error);

        // Assert
        var internalError = Assert.IsType<InternalError<string>>(result);

        Assert.Equal(error, internalError.Error);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromStringMessage_CreatesGenericInternalErrorInstanceWithError()
    {
        // Arrange
        const string expectedMessage = "Generic internal message";

        // Act
        IResult<string> result = Result<string>.InternalError(expectedMessage);

        // Assert
        var internalError = Assert.IsType<InternalError<string>>(result);

        Assert.Equal(expectedMessage, internalError.Error.Message);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InternalError_FromException_CreatesGenericInternalErrorInstanceWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Generic failure exception");

        // Act
        IResult<string> result = Result<string>.InternalError(exception);

        // Assert
        var internalError = Assert.IsType<InternalError<string>>(result);

        Assert.Same(exception, internalError.Error.Exception);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Invalid_CreatesGenericInvalidInstanceWithValidationState()
    {
        // Arrange
        var validationState = new ValidationState();

        // Act
        IResult<string> result = Result<string>.Invalid(validationState);

        // Assert
        var invalid = Assert.IsType<Invalid<string>>(result);

        Assert.Same(validationState, invalid.ValidationState);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }
}
