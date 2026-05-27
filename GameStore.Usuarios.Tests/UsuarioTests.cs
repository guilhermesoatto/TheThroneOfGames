using FluentAssertions;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Tests;

public class UsuarioTests
{
    private static Usuario CreateUsuario(string name = "Test User", string email = "test@example.com")
        => new(name, email, "hash", "User", Guid.NewGuid().ToString());

    [Fact]
    public void Usuario_Activation_Toggles_IsActive()
    {
        var usuario = CreateUsuario();
        usuario.IsActive.Should().BeFalse();

        usuario.Activate();

        usuario.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Usuario_UpdateProfile_Changes_Name_And_Email()
    {
        var usuario = CreateUsuario("Old", "old@example.com");
        usuario.UpdateProfile("New Name", "new@example.com");

        usuario.Name.Should().Be("New Name");
        usuario.Email.Should().Be("new@example.com");
    }

    [Fact]
    public void Usuario_UpdateRole_Changes_Role_Successfully()
    {
        var usuario = CreateUsuario();
        usuario.UpdateRole("Admin");

        usuario.Role.Should().Be("Admin");
    }

    [Fact]
    public void Usuario_UpdateRole_WithEmptyRole_ShouldThrow()
    {
        var usuario = CreateUsuario();
        var act = () => usuario.UpdateRole("");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("newRole");
    }

    [Fact]
    public void Usuario_Disable_Then_Enable_TogglesCorrectly()
    {
        var usuario = CreateUsuario();
        usuario.Activate();

        usuario.Disable();
        usuario.IsActive.Should().BeFalse();

        usuario.Enable();
        usuario.IsActive.Should().BeTrue();
    }
}
