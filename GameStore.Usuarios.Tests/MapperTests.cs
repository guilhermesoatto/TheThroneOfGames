using FluentAssertions;
using GameStore.Usuarios.Application.Mappers;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Tests;

public class MapperTests
{
    private static Usuario CreateUsuario(Guid? id = null, string name = "Test User",
        string email = "test@example.com", string role = "User", bool active = true)
        => new(id ?? Guid.NewGuid(), name, email, "hash", role, active, "token");

    [Fact]
    public void UsuarioMapper_ToDTO_ValidUsuario_ShouldMapCorrectly()
    {
        var usuario = CreateUsuario();
        var dto = UsuarioMapper.ToDTO(usuario);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(usuario.Id);
        dto.Name.Should().Be(usuario.Name);
        dto.Email.Should().Be(usuario.Email);
        dto.Role.Should().Be(usuario.Role);
        dto.IsActive.Should().Be(usuario.IsActive);
    }

    [Fact]
    public void UsuarioMapper_ToDTO_NullUsuario_ShouldThrow()
    {
        var act = () => UsuarioMapper.ToDTO(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UsuarioMapper_FromDTO_ValidDTO_ShouldMapCorrectly()
    {
        var dto = new UsuarioDTO
        {
            Id = Guid.NewGuid(), Name = "Test User",
            Email = "test@example.com", Role = "User",
            IsActive = true, CreatedAt = DateTime.UtcNow
        };

        var usuario = UsuarioMapper.FromDTO(dto);

        usuario.Should().NotBeNull();
        usuario.Id.Should().Be(dto.Id);
        usuario.Name.Should().Be(dto.Name);
        usuario.Email.Should().Be(dto.Email);
    }

    [Fact]
    public void UsuarioMapper_ToDTOList_ShouldMapAll()
    {
        var usuarios = new List<Usuario>
        {
            CreateUsuario(name: "User 1", email: "u1@e.com"),
            CreateUsuario(name: "User 2", email: "u2@e.com", active: false)
        };

        var dtos = UsuarioMapper.ToDTOList(usuarios).ToList();

        dtos.Should().HaveCount(2);
        dtos[0].Name.Should().Be("User 1");
        dtos[1].IsActive.Should().BeFalse();
    }
}
