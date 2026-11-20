namespace DannyGoodacre.Results.Tests;

public sealed class ValidationStateTests
{
    [Fact]
    public void InitialState_HasErrorsIsFalseAndErrorsIsEmpty()
    {
        // Act
        var state = new ValidationState();

        // Assert
        Assert.False(state.HasErrors);
        Assert.Empty(state.Errors);
    }

    [Fact]
    public void AddError_AddsErrorToSpecifiedProperty()
    {
        // Arrange
        var state = new ValidationState();

        // Act
        state.AddError("Name", new ValidationError("Name is required.", "Invalid"));

        // Assert
        Assert.True(state.HasErrors);
        Assert.True(state.Errors.ContainsKey("Name"));

        var item = Assert.Single(state.Errors["Name"]);
        Assert.Equal("Name is required.", item.Message);
    }

    [Fact]
    public void AddErrorIf_WhenConditionIsTrue_AddsErrorAndReturnsTrue()
    {
        // Arrange
        var state = new ValidationState();

        // Act
        var result = state.AddErrorIf(true, "Name", new ValidationError("Name is required.", "Invalid"));

        // Assert
        Assert.True(result);
        Assert.Equal("Name is required.", Assert.Single(state.Errors["Name"]).Message);
    }

    [Fact]
    public void AddErrorIf_WhenConditionIsFalse_AddsNothingAndReturnsFalse()
    {
        // Arrange
        var state = new ValidationState();
        var errors = state.Errors;

        // Act
        var result = state.AddErrorIf(false, "Name", new ValidationError("Name is required.", "Invalid"));

        // Assert
        Assert.False(result);
        Assert.False(state.HasErrors);
        Assert.Same(errors, state.Errors);
    }

    [Fact]
    public void AddError_IsCaseInsensitiveForPropertyNames()
    {
        // Arrange
        var state = new ValidationState();

        // Act
        state.AddError("Email", new ValidationError("Email is required.", "Invalid"));
        state.AddError("email", new ValidationError("Email format is invalid.", "Invalid"));

        // Assert
        Assert.Single(state.Errors);
        Assert.Equal(2, state.Errors["Email"].Count);
        Assert.Equal("Email is required.", state.Errors["Email"][0].Message);
        Assert.Equal("Email format is invalid.", state.Errors["Email"][1].Message);
    }

    [Fact]
    public void Merge_WhenOtherHasNoErrors_DoesNotModifyState()
    {
        // Arrange
        var state = new ValidationState();
        state.AddError("Age", new ValidationError("Must be positive.", "Invalid"));

        var other = new ValidationState();

        // Act
        state.Merge(other);

        // Assert
        Assert.Single(state.Errors);
        Assert.Equal("Must be positive.", state.Errors["Age"][0].Message);
    }

    [Fact]
    public void Merge_WhenMergingIntoItself_DuplicatesEachError()
    {
        // Arrange
        var state = new ValidationState();
        state.AddError("Email", new ValidationError("Email is required.", "Invalid"));
        state.AddError("Password", new ValidationError("Password is too short.", "Invalid"));

        // Act
        state.Merge(state);

        // Assert
        Assert.Equal(["Email is required.", "Email is required."], state.Errors["Email"].Select(e => e.Message));
        Assert.Equal(["Password is too short.", "Password is too short."], state.Errors["Password"].Select(e => e.Message));
    }

    [Fact]
    public void Merge_CombinesErrorsFromOtherInstance()
    {
        // Arrange
        var state = new ValidationState();
        state.AddError("Username", new ValidationError("Username is required.", "Invalid"));

        var other = new ValidationState();
        other.AddError("Username", new ValidationError("Username must be unique.", "Invalid"));
        other.AddError("Password", new ValidationError("Password is too short.", "Invalid"));

        // Act
        state.Merge(other);

        // Assert
        Assert.Equal(2, state.Errors.Count);
        Assert.Equal(2, state.Errors["Username"].Count);
        Assert.Single(state.Errors["Password"]);
    }

    [Fact]
    public void ToString_WhenNoErrors_ReturnsEmptyString()
    {
        // Arrange
        var state = new ValidationState();

        // Act
        string result = state.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_WhenHasErrors_ReturnsFormattedMultiLineString()
    {
        // Arrange
        var state = new ValidationState();
        state.AddError("Name", new ValidationError("Name is required.", "Invalid"));
        state.AddError("Name", new ValidationError("Name must be at least 3 characters.", "Invalid"));
        state.AddError("Age", new ValidationError("Must be 18 or older.", "Invalid"));

        string expected =
            "Name:" + Environment.NewLine +
            "  - Name is required." + Environment.NewLine +
            "  - Name must be at least 3 characters." + Environment.NewLine +
            "Age:" + Environment.NewLine +
            "  - Must be 18 or older.";

        // Act
        string result = state.ToString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Errors_IsCaseInsensitiveForLookups()
    {
        // Arrange
        var state = new ValidationState();

        state.AddError("Email", new ValidationError("Email is required.", "Invalid"));

        // Act & Assert
        Assert.True(state.Errors.ContainsKey("email"));
        Assert.Equal("Email is required.", Assert.Single(state.Errors["EMAIL"]).Message);
    }

    [Fact]
    public void Errors_DoesNotExposeTheInternalLists()
    {
        // Arrange
        var state = new ValidationState();

        state.AddError("Name", new ValidationError("Name is required.", "Invalid"));

        // Act
        IReadOnlyList<ValidationError> errors = state.Errors["Name"];

        // Assert
        Assert.IsNotType<List<ValidationError>>(errors, exactMatch: false);
    }

    [Fact]
    public void Errors_AfterAddError_ShowsTheNewError()
    {
        // Arrange
        var state = new ValidationState();

        state.AddError("Name", new ValidationError("Name is required.", "Invalid"));

        _ = state.Errors;

        // Act
        state.AddError("Age", new ValidationError("Must be positive.", "Invalid"));

        // Assert
        Assert.Equal(2, state.Errors.Count);
    }

    [Fact]
    public void AddError_WithValidationError_KeepsCodeAndArguments()
    {
        var state = new ValidationState();
        var error = new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" });

        state.AddError("Name", error);

        Assert.Equal(error, Assert.Single(state.Errors["Name"]));
        Assert.Equal("3", Assert.Single(state.Errors["Name"]).Arguments["Min"]);
    }

    [Fact]
    public void AddErrorIf_WithValidationError_AddsOnlyWhenConditionIsTrue()
    {
        var state = new ValidationState();
        var error = new ValidationError("Required.", "Required");

        Assert.False(state.AddErrorIf(false, "Name", error));
        Assert.False(state.HasErrors);

        Assert.True(state.AddErrorIf(true, "Name", error));
        Assert.Equal(error, Assert.Single(state.Errors["Name"]));
    }

    [Fact]
    public void Merge_KeepsCodesAndArguments()
    {
        var other = new ValidationState();
        var error = new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" });
        other.AddError("Name", error);

        var state = new ValidationState();
        state.Merge(other);

        Assert.Equal(error, Assert.Single(state.Errors["Name"]));
    }

    [Fact]
    public void ToString_PrintsOnlyTheMessage()
    {
        var state = new ValidationState();
        state.AddError("Name", new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" }));

        Assert.Equal("Name:" + Environment.NewLine + "  - Too short.", state.ToString());
    }
}
