using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Domain.Tests.Entities;

[TestClass]
public class UsuarioTests
{
    private static Usuario NewUsuario(string role = "User") =>
        new("Geralt", "geralt@rivia.test", "hash", role, "token-123");

    [TestMethod]
    public void Ctor_ShouldGenerateId_AndStartInactive()
    {
        var user = NewUsuario();

        Assert.AreNotEqual(Guid.Empty, user.Id);
        Assert.IsFalse(user.IsActive);
        Assert.AreEqual("Geralt", user.Name);
        Assert.AreEqual("geralt@rivia.test", user.Email);
        Assert.AreEqual("hash", user.PasswordHash);
        Assert.AreEqual("User", user.Role);
        Assert.AreEqual("token-123", user.ActiveToken);
    }

    [TestMethod]
    public void Ctor_WithExplicitState_ShouldPreserveAllFields()
    {
        var id = Guid.NewGuid();
        var user = new Usuario(id, "Ciri", "ciri@rivia.test", "h", "Admin", isActive: true, "tok");

        Assert.AreEqual(id, user.Id);
        Assert.AreEqual("Ciri", user.Name);
        Assert.AreEqual("Admin", user.Role);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var user = NewUsuario();

        user.Activate();

        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void DisableThenEnable_ShouldToggleIsActive()
    {
        var user = NewUsuario();
        user.Activate();

        user.Disable();
        Assert.IsFalse(user.IsActive);

        user.Enable();
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void UpdateRole_WithBlankRole_ShouldThrow(string? blank)
    {
        var user = NewUsuario();

        Assert.ThrowsExactly<ArgumentException>(() => user.UpdateRole(blank!));
    }

    [TestMethod]
    public void UpdateRole_WithValidRole_ShouldChangeRole()
    {
        var user = NewUsuario();

        user.UpdateRole("Admin");

        Assert.AreEqual("Admin", user.Role);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void ChangeRole_WithBlankRole_ShouldThrow(string blank)
    {
        var user = NewUsuario();

        Assert.ThrowsExactly<ArgumentException>(() => user.ChangeRole(blank));
    }

    [TestMethod]
    public void UpdateProfile_WithBlankName_ShouldThrow()
    {
        var user = NewUsuario();

        Assert.ThrowsExactly<ArgumentException>(() => user.UpdateProfile("  ", "new@mail.test"));
    }

    [TestMethod]
    public void UpdateProfile_WithBlankEmail_ShouldThrow()
    {
        var user = NewUsuario();

        Assert.ThrowsExactly<ArgumentException>(() => user.UpdateProfile("New Name", ""));
    }

    [TestMethod]
    public void UpdateProfile_WithValidData_ShouldUpdateNameAndEmail()
    {
        var user = NewUsuario();

        user.UpdateProfile("Yennefer", "yen@vengerberg.test");

        Assert.AreEqual("Yennefer", user.Name);
        Assert.AreEqual("yen@vengerberg.test", user.Email);
    }
}
