using Xunit;
using Moq;
using Domain.Interfaces;
using Application.Handlers;
using Application.Commands;
using Application.Interfaces;
using Domain.Entities;
using System.Threading.Tasks;

public class AuthTests
{
    [Fact]
    public async Task RegisterHandler_WhenUserExists_Throws()
    {
        var repo = new Mock<IRepository<User>>();
        repo.Setup(r => r.ListAsync()).ReturnsAsync(new[] { new User { Username = "bob", Email = "bob@x" } as User });
        var uow = new Mock<IUnitOfWork>();
        var hasher = new Mock<IPasswordHasher>();
        var tokens = new Mock<ITokenService>();

        var handler = new RegisterHandler(repo.Object, uow.Object, hasher.Object, tokens.Object);
        await Assert.ThrowsAsync<System.ApplicationException>(() => handler.Handle(new RegisterCommand(new Application.DTOs.RegisterRequest("bob","bob@x","pw")), System.Threading.CancellationToken.None));
    }
}
