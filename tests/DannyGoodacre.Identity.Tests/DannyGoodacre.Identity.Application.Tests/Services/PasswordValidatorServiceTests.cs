using DannyGoodacre.Identity.Application.Services;
using DannyGoodacre.Identity.Configuration;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Microsoft.Extensions.Options;

namespace DannyGoodacre.Identity.Application.Tests.Services;

[TestFixture]
public sealed class PasswordValidatorServiceTests : TestBase
{
    private ValidationState _requestValidationState = null!;

    private string _requestPassword = null!;

    private IdentityOptions _testOptions = null!;

    private PasswordValidatorService _target = null!;

    [Test]
    public void IsPasswordValid_WhenPasswordEmpty_ShouldReturnInvalid()
    {
        // Arrange
        _requestValidationState = new ValidationState();

        _requestPassword = string.Empty;

        _testOptions = new IdentityOptions
        {
            PasswordValidator = new PasswordValidatorOptions()
        };

        _target = new PasswordValidatorService(Options.Create(_testOptions));

        var expectedValidationState = new ValidationState();

        expectedValidationState.AddError("password", "Must not be null, empty, or whitespace.");

        // Act
        Result result = _target.IsPasswordValid(_requestValidationState, _requestPassword);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(result);

            Assert.That(result.ValidationState, Is.EqualTo(expectedValidationState).UsingPropertiesComparer());
        }
    }

    [Test]
    public void IsPasswordValid_WhenPasswordAllLowercase_ShouldReturnInvalid()
    {
        // Arrange
        _requestValidationState = new ValidationState();

        _requestPassword = "password";

        _testOptions = new IdentityOptions
        {
            PasswordValidator = new PasswordValidatorOptions
            {
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                MinimumLength = 10
            }
        };

        _target = new PasswordValidatorService(Options.Create(_testOptions));

        var expectedValidationState = new ValidationState();

        expectedValidationState.AddError("Password", "Must contain at least one uppercase character.");
        expectedValidationState.AddError("Password", "Must contain at least one digit.");
        expectedValidationState.AddError("Password", "Must contain at least one non-alphanumeric character.");
        expectedValidationState.AddError("Password", "Must be at least 10 characters long.");

        // Act
        Result result = _target.IsPasswordValid(_requestValidationState, _requestPassword);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(result);

            Assert.That(result.ValidationState, Is.EqualTo(expectedValidationState).UsingPropertiesComparer());
        }
    }

    [Test]
    public void IsPasswordValid_WhenPasswordAllDigits_ShouldReturnInvalid()
    {
        // Arrange
        _requestValidationState = new ValidationState();

        _requestPassword = "1234";

        _testOptions = new IdentityOptions
        {
            PasswordValidator = new PasswordValidatorOptions
            {
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                MinimumLength = 10
            }
        };

        _target = new PasswordValidatorService(Options.Create(_testOptions));

        var expectedValidationState = new ValidationState();

        expectedValidationState.AddError("Password", "Must contain at least one lowercase character.");
        expectedValidationState.AddError("Password", "Must contain at least one uppercase character.");
        expectedValidationState.AddError("Password", "Must contain at least one non-alphanumeric character.");
        expectedValidationState.AddError("Password", "Must be at least 10 characters long.");

        // Act
        Result result = _target.IsPasswordValid(_requestValidationState, _requestPassword);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(result);

            Assert.That(result.ValidationState, Is.EqualTo(expectedValidationState).UsingPropertiesComparer());
        }
    }

    [Test]
    public void IsPasswordValid_WhenPasswordAllUppercase_ShouldReturnInvalid()
    {
        // Arrange
        _requestValidationState = new ValidationState();

        _requestPassword = "PASSWORD";

        _testOptions = new IdentityOptions
        {
            PasswordValidator = new PasswordValidatorOptions
            {
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                MinimumLength = 10
            }
        };

        _target = new PasswordValidatorService(Options.Create(_testOptions));

        var expectedValidationState = new ValidationState();

        expectedValidationState.AddError("Password", "Must contain at least one lowercase character.");
        expectedValidationState.AddError("Password", "Must contain at least one digit.");
        expectedValidationState.AddError("Password", "Must contain at least one non-alphanumeric character.");
        expectedValidationState.AddError("Password", "Must be at least 10 characters long.");

        // Act
        Result result = _target.IsPasswordValid(_requestValidationState, _requestPassword);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(result);

            Assert.That(result.ValidationState, Is.EqualTo(expectedValidationState).UsingPropertiesComparer());
        }
    }

    [Test]
    public void IsPasswordValid_WhenPasswordAllNonAlphanumeric_ShouldReturnInvalid()
    {
        // Arrange
        _requestValidationState = new ValidationState();

        _requestPassword = "$%^*";

        _testOptions = new IdentityOptions
        {
            PasswordValidator = new PasswordValidatorOptions
            {
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                MinimumLength = 3
            }
        };

        _target = new PasswordValidatorService(Options.Create(_testOptions));

        var expectedValidationState = new ValidationState();

        expectedValidationState.AddError("Password", "Must contain at least one lowercase character.");
        expectedValidationState.AddError("Password", "Must contain at least one uppercase character.");
        expectedValidationState.AddError("Password", "Must contain at least one digit.");

        // Act
        Result result = _target.IsPasswordValid(_requestValidationState, _requestPassword);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(result);

            Assert.That(result.ValidationState, Is.EqualTo(expectedValidationState).UsingPropertiesComparer());
        }
    }

    [Test]
    public void IsPasswordValid_WhenPasswordValid_ShouldReturnSuccess()
    {
        // Arrange
        _requestValidationState = new ValidationState();

        _requestPassword = "TestPassword123$";

        _testOptions = new IdentityOptions
        {
            PasswordValidator = new PasswordValidatorOptions
            {
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                MinimumLength = 10
            }
        };

        _target = new PasswordValidatorService(Options.Create(_testOptions));

        // Act
        Result result = _target.IsPasswordValid(_requestValidationState, _requestPassword);

        // Assert
        AssertSuccess(result);
    }
}
