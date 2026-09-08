namespace DannyGoodacre.Primitives.Tests;

[TestFixture]
public class ErrorTests : TestBase
{
    [Test]
    public void ImplicitString()
    {
        // Arrange
        const string testErrorMessage = "Test Error Message";

        // Act
        Error result = testErrorMessage;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<Error>());

            Assert.That(result.Message, Is.EqualTo(testErrorMessage));
        }
    }

    [Test]
    public void ImplicitException()
    {
        // Arrange
        var testException = new Exception("Test Exception Message");

        // Act
        Error result = testException;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<Error>());

            Assert.That(result.Message, Is.EqualTo(testException.Message));
        }
    }
}
