namespace DannyGoodacre.Cqrs.Tests;

public sealed class TypeNameFormatterTests
{
    public sealed class Outer<T>
    {
        public sealed class Inner;
    }

    [Theory]
    [InlineData(typeof(string), "String")]
    [InlineData(typeof(List<int>), "List<Int32>")]
    [InlineData(typeof(Dictionary<string, List<Guid>>), "Dictionary<String, List<Guid>>")]
    [InlineData(typeof(List<>), "List<T>")]
    [InlineData(typeof(Outer<int>.Inner), "Inner<Int32>")]
    public void Format_ReturnsTheNameWithTypeArgumentsInAngleBrackets(Type type, string expected)
    {
        // Act
        string name = TypeNameFormatter.Format(type);

        // Assert
        Assert.Equal(expected, name);
    }
}
