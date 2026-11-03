namespace DannyGoodacre.Primitives.Tests;

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
        state.AddError("Name", "Name is required.");

        // Assert
        Assert.True(state.HasErrors);
        Assert.True(state.Errors.ContainsKey("Name"));

        var item = Assert.Single(state.Errors["Name"]);
        Assert.Equal("Name is required.", item);
    }

    [Fact]
    public void AddError_IsCaseInsensitiveForPropertyNames()
    {
        // Arrange
        var state = new ValidationState();

        // Act
        state.AddError("Email", "Email is required.");
        state.AddError("email", "Email format is invalid.");

        // Assert
        Assert.Single(state.Errors);
        Assert.Equal(2, state.Errors["Email"].Count);
        Assert.Equal("Email is required.", state.Errors["Email"][0]);
        Assert.Equal("Email format is invalid.", state.Errors["Email"][1]);
    }

    [Fact]
    public void Merge_WhenOtherHasNoErrors_DoesNotModifyState()
    {
        // Arrange
        var state = new ValidationState();
        state.AddError("Age", "Must be positive.");

        var other = new ValidationState();

        // Act
        state.Merge(other);

        // Assert
        Assert.Single(state.Errors);
        Assert.Equal("Must be positive.", state.Errors["Age"][0]);
    }

    [Fact]
    public void Merge_CombinesErrorsFromOtherInstance()
    {
        // Arrange
        var state = new ValidationState();
        state.AddError("Username", "Username is required.");

        var other = new ValidationState();
        other.AddError("Username", "Username must be unique.");
        other.AddError("Password", "Password is too short.");

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
        state.AddError("Name", "Name is required.");
        state.AddError("Name", "Name must be at least 3 characters.");
        state.AddError("Age", "Must be 18 or older.");

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

        state.AddError("Email", "Email is required.");

        // Act & Assert
        Assert.True(state.Errors.ContainsKey("email"));
        Assert.Equal("Email is required.", Assert.Single(state.Errors["EMAIL"]));
    }

    [Fact]
    public void Errors_DoesNotExposeTheInternalLists()
    {
        // Arrange
        var state = new ValidationState();

        state.AddError("Name", "Name is required.");

        // Act
        IReadOnlyList<string> errors = state.Errors["Name"];

        // Assert
        Assert.IsNotType<List<string>>(errors, exactMatch: false);
    }

    [Fact]
    public void Errors_AfterAddError_ShowsTheNewError()
    {
        // Arrange
        var state = new ValidationState();

        state.AddError("Name", "Name is required.");

        _ = state.Errors;

        // Act
        state.AddError("Age", "Must be positive.");

        // Assert
        Assert.Equal(2, state.Errors.Count);
    }
}
