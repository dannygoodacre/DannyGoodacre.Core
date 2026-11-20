namespace DannyGoodacre.Results.Tests;

public sealed class ValidationErrorTests
{
    [Fact]
    public void Constructor_WithMessageOnly_UsesDefaultCodeAndNoArguments()
    {
        var error = new ValidationError("Bad.");

        Assert.Equal("Bad.", error.Message);
        Assert.Equal("General", error.Code);
        Assert.Empty(error.Arguments);
    }

    [Fact]
    public void Constructor_KeepsMessageCodeAndArguments()
    {
        var error = new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" });

        Assert.Equal("Too short.", error.Message);
        Assert.Equal("TooShort", error.Code);
        Assert.Equal("3", error.Arguments["Min"]);
    }

    [Fact]
    public void Default_ReturnsDefaultValues()
    {
        ValidationError error = default;

        Assert.Equal("An unspecified error occurred.", error.Message);
        Assert.Equal("General", error.Code);
        Assert.NotNull(error.Arguments);
        Assert.Empty(error.Arguments);
        Assert.Equal(new ValidationError("An unspecified error occurred."), error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_WithBlankMessageOrCode_UsesDefaults(string? blank)
    {
        var error = new ValidationError(blank!, blank!);

        Assert.Equal("An unspecified error occurred.", error.Message);
        Assert.Equal("General", error.Code);
    }

    [Fact]
    public void Constructor_CopiesTheArguments()
    {
        var arguments = new Dictionary<string, string> { ["Min"] = "3" };
        var error = new ValidationError("Too short.", "TooShort", arguments);

        arguments["Min"] = "5";

        Assert.Equal("3", error.Arguments["Min"]);
    }

    [Fact]
    public void Equals_ComparesArgumentsByContent()
    {
        var a = new ValidationError("m", "c", new Dictionary<string, string> { ["X"] = "1", ["Y"] = "y" });
        var b = new ValidationError("m", "c", new Dictionary<string, string> { ["Y"] = "y", ["X"] = "1" });

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_WhenArgumentsDiffer_ReturnsFalse()
    {
        var a = new ValidationError("m", "c", new Dictionary<string, string> { ["X"] = "1" });

        Assert.NotEqual(a, new ValidationError("m", "c", new Dictionary<string, string> { ["X"] = "2" }));
        Assert.NotEqual(a, new ValidationError("m", "c", new Dictionary<string, string> { ["Z"] = "1" }));
        Assert.NotEqual(a, new ValidationError("m", "c"));
    }

    [Fact]
    public void Equals_WhenMessageOrCodeDiffer_ReturnsFalse()
    {
        var a = new ValidationError("m", "c");

        Assert.NotEqual(a, new ValidationError("other", "c"));
        Assert.NotEqual(a, new ValidationError("m", "other"));
    }

    [Fact]
    public void ToString_ReturnsTheMessage()
        => Assert.Equal("Bad.", new ValidationError("Bad.", "Code").ToString());
}
