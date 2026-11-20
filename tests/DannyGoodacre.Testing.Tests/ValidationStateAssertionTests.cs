using DannyGoodacre.Results;
using Xunit.Sdk;

namespace DannyGoodacre.Testing.Tests;

public sealed class ValidationStateAssertionTests
{
    private static readonly ValidationError TooShort =
        new("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" });

    private static readonly ValidationError Required = new("Required.", "Required");

    private static ValidationState StateWith(params ValidationError[] errors)
    {
        var state = new ValidationState();

        foreach (ValidationError error in errors)
        {
            state.AddError("Name", error);
        }

        return state;
    }

    [Fact]
    public void ShouldHaveNoErrors_WhenEmpty_Passes() => new ValidationState().ShouldHaveNoErrors();

    [Fact]
    public void ShouldHaveNoErrors_WhenErrors_Throws()
        => Assert.Throws<FalseException>(() => StateWith(Required).ShouldHaveNoErrors());

    [Fact]
    public void ShouldHaveErrorCode_WhenCodePresentAmongOthers_Passes()
        => StateWith(Required, TooShort).ShouldHaveErrorCode("name", "TooShort");

    [Fact]
    public void ShouldHaveErrorCode_WhenCodeMissing_ThrowsNamingPropertyAndFoundCodes()
    {
        var ex = Assert.Throws<TrueException>(() => StateWith(Required).ShouldHaveErrorCode("Name", "TooShort"));

        Assert.Contains("'Name'", ex.Message);
        Assert.Contains("[Required]", ex.Message);
    }

    [Fact]
    public void ShouldHaveErrorCode_WhenPropertyMissing_Throws()
        => Assert.Throws<TrueException>(() => StateWith(Required).ShouldHaveErrorCode("Other", "Required"));

    [Fact]
    public void ShouldHaveError_WhenEqualIncludingArguments_Passes()
        => StateWith(Required, TooShort).ShouldHaveError(
            "Name",
            new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" }));

    [Fact]
    public void ShouldHaveError_WhenArgumentsDiffer_Throws()
        => Assert.Throws<TrueException>(() => StateWith(TooShort).ShouldHaveError(
            "Name",
            new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "4" })));

    [Fact]
    public void ShouldHaveErrorCodesFor_WhenExactOrder_Passes()
        => StateWith(Required, TooShort).ShouldHaveErrorCodesFor("Name", "Required", "TooShort");

    [Theory]
    [InlineData("TooShort", "Required")]
    [InlineData("Required")]
    [InlineData("Required", "TooShort", "Extra")]
    public void ShouldHaveErrorCodesFor_WhenDifferent_ThrowsListingFoundCodes(params string[] codes)
    {
        var ex = Assert.Throws<TrueException>(() => StateWith(Required, TooShort).ShouldHaveErrorCodesFor("Name", codes));

        Assert.Contains("[Required, TooShort]", ex.Message);
    }

    [Fact]
    public void ShouldHaveErrorsFor_WhenExact_Passes()
        => StateWith(Required, TooShort).ShouldHaveErrorsFor("Name", Required, TooShort);

    [Fact]
    public void ShouldHaveErrorsFor_WhenDifferent_Throws()
        => Assert.Throws<TrueException>(() => StateWith(Required, TooShort).ShouldHaveErrorsFor("Name", TooShort, Required));

    [Fact]
    public void ShouldMatch_WhenSameContent_Passes()
        => StateWith(Required, TooShort).ShouldMatch(StateWith(Required, TooShort));

    [Fact]
    public void ShouldMatch_WhenArgumentsDiffer_Throws()
        => Assert.Throws<TrueException>(() => StateWith(TooShort).ShouldMatch(
            StateWith(new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "9" }))));

    [Fact]
    public void ShouldMatch_WhenPropertyCountDiffers_Throws()
    {
        var expected = StateWith(Required);
        expected.AddError("Other", Required);

        Assert.Throws<EqualException>(() => StateWith(Required).ShouldMatch(expected));
    }

    [Fact]
    public void ShouldBeInvalid_WithExpectedState_UsesErrorContentEquality()
    {
        IResult result = new Invalid(StateWith(TooShort));

        result.ShouldBeInvalid(StateWith(new ValidationError("Too short.", "TooShort", new Dictionary<string, string> { ["Min"] = "3" })));
    }
}
