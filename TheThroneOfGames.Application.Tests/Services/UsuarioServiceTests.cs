using Moq;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Domain.Interfaces;

namespace TheThroneOfGames.Application.Tests.Services;

[TestClass]
public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repo = new();
    private readonly Mock<IEventBus> _bus = new();
    private UsuarioService CreateSut() => new(_repo.Object, _bus.Object);

    // ---- PreRegister: validação de senha ----------------------------------

    [TestMethod]
    [DataRow("short1!", "at least 8 characters")]
    [DataRow("onlyletters", "one number and one special character")]
    [DataRow("12345678", "one letter and one special character")]
    [DataRow("!!!!!!!!", "one letter and one number")]
    [DataRow("letters1234", "at least one special character")]
    [DataRow("letters!!!!", "at least one number")]
    [DataRow("1234!!!!", "at least one letter")]
    public async Task PreRegister_WithWeakPassword_ShouldThrowWithReason(string password, string expectedFragment)
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => sut.PreRegisterUserAsync("a@b.test", "Alice", password));

        StringAssert.Contains(ex.Message, expectedFragment);
        _repo.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [TestMethod]
    public async Task PreRegister_WithBlankEmail_ShouldThrow()
    {
        var sut = CreateSut();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => sut.PreRegisterUserAsync("  ", "Alice", "Str0ng!Pass"));
    }

    [TestMethod]
    public async Task PreRegister_Valid_ShouldPersistInactiveUser_AndReturnToken()
    {
        Usuario? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
             .Callback<Usuario>(u => saved = u)
             .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var token = await sut.PreRegisterUserAsync("a@b.test", "Alice", "Str0ng!Pass", "Admin");

        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        Assert.IsNotNull(saved);
        Assert.AreEqual("Admin", saved!.Role);
        Assert.IsFalse(saved.IsActive);
        Assert.AreNotEqual("Str0ng!Pass", saved.PasswordHash, "a senha deve ser hasheada, nunca persistida em claro");
        Assert.AreEqual(token, saved.ActiveToken);
        _repo.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [TestMethod]
    public async Task PreRegister_WithoutRole_ShouldDefaultToUser()
    {
        Usuario? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
             .Callback<Usuario>(u => saved = u).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.PreRegisterUserAsync("a@b.test", "Alice", "Str0ng!Pass");

        Assert.AreEqual("User", saved!.Role);
    }

    // ---- Activate --------------------------------------------------------

    [TestMethod]
    public async Task Activate_WithUnknownToken_ShouldThrow()
    {
        _repo.Setup(r => r.GetByActivationTokenAsync("nope")).ReturnsAsync((Usuario?)null);
        var sut = CreateSut();

        await Assert.ThrowsExactlyAsync<Exception>(() => sut.ActivateUserAsync("nope"));
    }

    [TestMethod]
    public async Task Activate_Valid_ShouldActivateUser_PublishEvent_AndPersist()
    {
        var user = new Usuario("Alice", "a@b.test", "hash", "User", "tok");
        _repo.Setup(r => r.GetByActivationTokenAsync("tok")).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.ActivateUserAsync("tok");

        Assert.IsTrue(user.IsActive);
        _bus.Verify(b => b.PublishAsync(It.IsAny<UsuarioAtivadoEvent>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    // ---- Profile / role / enable-disable -------------------------------

    [TestMethod]
    public async Task UpdateUserProfile_WithBlankExistingEmail_ShouldThrow()
    {
        var sut = CreateSut();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => sut.UpdateUserProfileAsync("", "New", "new@b.test"));
    }

    [TestMethod]
    public async Task UpdateUserProfile_WhenUserNotFound_ShouldThrow()
    {
        _repo.Setup(r => r.GetByEmailAsync("ghost@b.test")).ReturnsAsync((Usuario?)null);
        var sut = CreateSut();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => sut.UpdateUserProfileAsync("ghost@b.test", "New", "new@b.test"));
    }

    [TestMethod]
    public async Task UpdateUserProfile_Valid_ShouldUpdate_PublishEvent_AndPersist()
    {
        var user = new Usuario("Alice", "a@b.test", "hash", "User", "tok");
        _repo.Setup(r => r.GetByEmailAsync("a@b.test")).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.UpdateUserProfileAsync("a@b.test", "Alice II", "alice2@b.test");

        Assert.AreEqual("Alice II", user.Name);
        Assert.AreEqual("alice2@b.test", user.Email);
        _bus.Verify(b => b.PublishAsync(It.IsAny<UsuarioPerfillAtualizadoEvent>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [TestMethod]
    public async Task GetUserById_WhenNotFound_ShouldThrow()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);
        var sut = CreateSut();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => sut.GetUserByIdAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task UpdateUserRole_ShouldChangeRole_AndPersist()
    {
        var user = new Usuario("Alice", "a@b.test", "hash", "User", "tok");
        _repo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.UpdateUserRoleAsync(user.Id, "Admin");

        Assert.AreEqual("Admin", user.Role);
        _repo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [TestMethod]
    public async Task DisableUser_ThenEnableUser_ShouldPersistBothTransitions()
    {
        var user = new Usuario("Alice", "a@b.test", "hash", "User", "tok");
        user.Activate();
        _repo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.DisableUserAsync(user.Id);
        Assert.IsFalse(user.IsActive);

        await sut.EnableUserAsync(user.Id);
        Assert.IsTrue(user.IsActive);

        _repo.Verify(r => r.UpdateAsync(user), Times.Exactly(2));
    }
}
