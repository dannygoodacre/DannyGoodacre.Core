using DannyGoodacre.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DannyGoodacre.Identity.Tests.Extensions;

[TestFixture]
public class ApplicationBuilderExtensionsTests : TestBase
{
    private string _requestUsername = null!;

    private string _requestPassword = null!;

    private bool _testDoesUserRoleExist;

    private bool _testDoesAdminRoleExist;

    private IdentityResult _testCreateUserRoleResult = null!;

    private IdentityResult _testCreateAdminRoleResult = null!;

    private Core.IdentityUser _testAdminUser = null!;

    private IdentityResult _testCreateUserResult = null!;

    private IdentityResult _testAddToRoleResult = null!;

    private Mock<UserManager<Core.IdentityUser>> _userManagerMock = null!;

    private Mock<RoleManager<IdentityRole>> _roleManagerMock = null!;

    // We can use a mock instance since we're testing an extension method.
    private Mock<IApplicationBuilder> _applicationBuilderMock = null!;

    [SetUp]
    public void SetUp()
    {
        _requestUsername = "Request Username";

        _requestPassword = "Request Password";

        _testDoesUserRoleExist = false;

        _testDoesAdminRoleExist = false;

        _testCreateUserRoleResult = IdentityResult.Success;

        _testCreateAdminRoleResult = IdentityResult.Success;

        _testAdminUser = null!;

        _testCreateUserResult = IdentityResult.Success;

        _testAddToRoleResult = IdentityResult.Success;

        var userStore = new Mock<IUserStore<Core.IdentityUser>>();

        _userManagerMock = new Mock<UserManager<Core.IdentityUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock
            .SetupSet(x => x.Logger = null!)
            .Verifiable(Times.Once);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();

        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _roleManagerMock
            .SetupSet(x => x.Logger = null!)
            .Verifiable(Times.Once);

        var servicesMock = new Mock<IServiceProvider>();

        servicesMock
            .Setup(x => x.GetService(typeof(UserManager<Core.IdentityUser>)))
            .Returns(_userManagerMock.Object);

        servicesMock
            .Setup(x => x.GetService(typeof(RoleManager<IdentityRole>)))
            .Returns(_roleManagerMock.Object);

        var scopeMock = new Mock<IServiceScope>();

        scopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(servicesMock.Object);

        scopeMock.Setup(x => x.Dispose()).Verifiable();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(scopeMock.Object);

        servicesMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        _applicationBuilderMock = new Mock<IApplicationBuilder>();

        _applicationBuilderMock
            .Setup(x => x.ApplicationServices)
            .Returns(servicesMock.Object);
    }

    private Task<Result> Act()
        => _applicationBuilderMock.Object.SeedIdentityAsync(_requestUsername, _requestPassword);

    [Test]
    public async Task SeedIdentityAsync_WhenCreateRoleFails_ShouldReturnInternalError()
    {
        // Arrange
        SetupRoleManager_RoleExistsAsync_User();

        _testCreateUserRoleResult = IdentityResult.Failed();

        SetupRoleManager_CreateAsync_User();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, "User role creation failed.");
    }

    [Test]
    public async Task SeedIdentityAsync_WhenAdminUserAlreadyExists_ShouldReturnSuccess()
    {
        // Arrange
        SetupRoleManager_RoleExistsAsync_User();

        SetupRoleManager_CreateAsync_User();

        SetupRoleManager_RoleExistsAsync_Admin();

        SetupRoleManager_CreateAsync_Admin();

        _testAdminUser = new Core.IdentityUser();

        SetupUserManager_FindByNameAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task SeedIdentityAsync_WhenCreateUserFails_ShouldReturnInternalError()
    {
        // Arrange
        SetupRoleManager_RoleExistsAsync_User();

        SetupRoleManager_CreateAsync_User();

        SetupRoleManager_RoleExistsAsync_Admin();

        SetupRoleManager_CreateAsync_Admin();

        SetupUserManager_FindByNameAsync();

        _testCreateUserResult = IdentityResult.Failed();

        SetupUserManager_CreateAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, "User creation failed.");
    }

    [Test]
    public async Task SeedIdentityAsync_WhenAddToRoleFails_ShouldReturnInternalError()
    {
        // Arrange
        SetupRoleManager_RoleExistsAsync_User();

        SetupRoleManager_CreateAsync_User();

        SetupRoleManager_RoleExistsAsync_Admin();

        SetupRoleManager_CreateAsync_Admin();

        SetupUserManager_FindByNameAsync();

        SetupUserManager_CreateAsync();

        _testAddToRoleResult = IdentityResult.Failed();

        SetupUserManager_AddToRoleAsync();

        // Act
        var result = await Act();

        // Assert
        AssertInternalError(result, "Role creation failed.");
    }

    [Test]
    public async Task SeedIdentityAsync_WhenSuccessfulAndRolesAlreadyExist_ShouldReturnSuccess()
    {
        // Arrange
        _testDoesUserRoleExist = true;

        SetupRoleManager_RoleExistsAsync_User();

        _testDoesAdminRoleExist = true;

        SetupRoleManager_RoleExistsAsync_Admin();

        SetupUserManager_FindByNameAsync();

        SetupUserManager_CreateAsync();

        SetupUserManager_AddToRoleAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    [Test]
    public async Task SeedIdentityAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        SetupRoleManager_RoleExistsAsync_User();

        SetupRoleManager_CreateAsync_User();

        SetupRoleManager_RoleExistsAsync_Admin();

        SetupRoleManager_CreateAsync_Admin();

        SetupUserManager_FindByNameAsync();

        SetupUserManager_CreateAsync();

        SetupUserManager_AddToRoleAsync();

        // Act
        var result = await Act();

        // Assert
        AssertSuccess(result);
    }

    private void SetupRoleManager_RoleExistsAsync_User()
        => _roleManagerMock
            .Setup(x => x.RoleExistsAsync(
                It.Is<string>(y => y == "User")))
            .ReturnsAsync(_testDoesUserRoleExist)
            .Verifiable(Times.Once);

    private void SetupRoleManager_RoleExistsAsync_Admin()
        => _roleManagerMock
            .Setup(x => x.RoleExistsAsync(
                It.Is<string>(y => y == "Admin")))
            .ReturnsAsync(_testDoesAdminRoleExist)
            .Verifiable(Times.Once);

    private void SetupRoleManager_CreateAsync_User()
        => _roleManagerMock
            .Setup(x => x.CreateAsync(
                It.Is<IdentityRole>(y => y.Name == "User")))
            .ReturnsAsync(_testCreateUserRoleResult)
            .Verifiable(Times.Once);

    private void SetupRoleManager_CreateAsync_Admin()
        => _roleManagerMock
            .Setup(x => x.CreateAsync(
                It.Is<IdentityRole>(y => y.Name == "Admin")))
            .ReturnsAsync(_testCreateAdminRoleResult)
            .Verifiable(Times.Once);


    private void SetupUserManager_FindByNameAsync()
        => _userManagerMock
            .Setup(x => x.FindByNameAsync(
                It.Is<string>(y => y == _requestUsername)))
            .ReturnsAsync(_testAdminUser)
            .Verifiable(Times.Once);

    private void SetupUserManager_CreateAsync()
        => _userManagerMock
            .Setup(x => x.CreateAsync(
                It.Is<Core.IdentityUser>(y => y.UserName ==  _requestUsername),
                It.Is<string>(y => y == _requestPassword)))
            .ReturnsAsync(_testCreateUserResult)
            .Verifiable(Times.Once);

    private void SetupUserManager_AddToRoleAsync()
        => _userManagerMock
            .Setup(x => x.AddToRoleAsync(
                It.Is<Core.IdentityUser>(y => y.UserName == _requestUsername),
                It.Is<string>(y => y == "Admin")))
            .ReturnsAsync(_testAddToRoleResult)
            .Verifiable(Times.Once);
}
