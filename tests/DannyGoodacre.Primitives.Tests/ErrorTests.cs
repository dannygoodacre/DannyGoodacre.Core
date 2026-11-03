namespace DannyGoodacre.Primitives.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndDefaultCodeAndNullException()
    {
        // Act
        var error = new Error("Something went wrong");

        // Assert
        Assert.Equal("Something went wrong", error.Message);
        Assert.Equal("General", error.Code);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void Constructor_WithMessageAndCode_SetsMessageAndCode()
    {
        // Act
        var error = new Error("User not found", "USER_NOT_FOUND");

        // Assert
        Assert.Equal("User not found", error.Message);
        Assert.Equal("USER_NOT_FOUND", error.Code);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void Constructor_WithException_SetsMessageCodeFromTypeAndExceptionInstance()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid state transition");

        // Act
        var error = new Error(exception);

        // Assert
        Assert.Equal("Invalid state transition", error.Message);
        Assert.Equal(nameof(InvalidOperationException), error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void Constructor_WithExceptionAndCustomCode_SetsMessageAndCustomCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid state transition");

        // Act
        var error = new Error(exception, "CUSTOM_STATE_ERROR");

        // Assert
        Assert.Equal("Invalid state transition", error.Message);
        Assert.Equal("CUSTOM_STATE_ERROR", error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceMessage_SetsDefaultFallbackMessage(string? message)
    {
        // Act
        var error = new Error(message!);

        // Assert
        Assert.Equal("An unspecified error occurred.", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceCode_SetsDefaultGeneralCode(string? code)
    {
        // Act
        var error = new Error("Error message", code!);

        // Assert
        Assert.Equal("General", error.Code);
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesErrorWithDefaultCode()
    {
        // Act
        Error error = "Conversion test message";

        // Assert
        Assert.Equal("Conversion test message", error.Message);
        Assert.Equal("General", error.Code);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void ImplicitConversion_FromException_CreatesErrorWithExceptionTypeAsCode()
    {
        // Arrange
        var exception = new ArgumentNullException("paramName");

        // Act
        Error error = exception;

        // Assert
        Assert.Equal(exception.Message, error.Message);
        Assert.Equal(nameof(ArgumentNullException), error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void ToString_WhenCodeIsGeneral_ReturnsMessageOnly()
    {
        // Act
        var error = new Error("Simple error message");

        // Assert
        Assert.Equal("Simple error message", error.ToString());
    }

    [Fact]
    public void ToString_WhenCodeIsNotGeneral_ReturnsFormattedCodeAndMessage()
    {
        // Act
        var error = new Error("Access denied", "AUTH_001");

        // Assert
        Assert.Equal("[AUTH_001] Access denied", error.ToString());
    }

    [Fact]
    public void Default_ReturnsFallbackMessageAndCode()
    {
        // Act
        Error error = default;

        // Assert
        Assert.Equal("An unspecified error occurred.", error.Message);
        Assert.Equal("General", error.Code);
        Assert.Equal("An unspecified error occurred.", error.ToString());
    }

    [Fact]
    public void Default_EqualsErrorWithFallbackValues()
    {
        // Act & Assert
        Assert.Equal(new Error(""), default(Error));
        Assert.Equal(new Error("").GetHashCode(), default(Error).GetHashCode());
    }

    [Fact]
    public void Constructor_WithNullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Error((Exception)null!));
        Assert.Throws<ArgumentNullException>(() => new Error((Exception)null!, "CODE"));
    }
}
