using DannyGoodacre.Cqrs.Testing;
using DannyGoodacre.Primitives;
using DannyGoodacre.Testing;
using Moq;

namespace CqrsTest.Tests;

public class UnitTest1 : IDisposable
{
    private readonly StateCommandHandlerTestContext<CreateUserHandler> _context = new();

    private readonly CreateUserHandler _handler;

    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);

    private readonly Mock<IUserRepository> _userRepositoryMock;

    public UnitTest1()
    {
        _userRepositoryMock = _mockRepository.Create<IUserRepository>();

        _handler = new CreateUserHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Test1()
    {
        // Arrange
        var command = new CreateUserCommand("TestUsername", "test@email.com");


        _userRepositoryMock.Setup(x => x.Add(It.IsAny<User>()));

        // Act
        IResult result = await _handler.HandleAsync(command, _cancellationToken);

        // Assert
        result.ShouldBeSuccess();
    }

    public void Dispose()
    {
        _mockRepository.VerifyAll();
        _mockRepository.VerifyNoOtherCalls();

        GC.SuppressFinalize(this);
    }
}
