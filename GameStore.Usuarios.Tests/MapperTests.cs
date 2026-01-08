using NUnit.Framework;
using Moq;
using GameStore.Usuarios.Application.Mappers;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Tests
{
    [TestFixture]
    public class MapperTests
    {
        #region UsuarioMapper Tests

        [Test]
        public void UsuarioMapper_ToDTO_ValidUsuario_ShouldMapCorrectly()
        {
            // Arrange
            var usuario = new Usuario(
                id: Guid.NewGuid(),
                name: "Test User",
                email: "test@example.com",
                passwordHash: "hashedpassword",
                role: "User",
                isActive: true,
                activeToken: "token123"
            );

            // Act
            var result = UsuarioMapper.ToDTO(usuario);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(usuario.Id));
            Assert.That(result.Name, Is.EqualTo(usuario.Name));
            Assert.That(result.Email, Is.EqualTo(usuario.Email));
            Assert.That(result.Role, Is.EqualTo(usuario.Role));
            Assert.That(result.IsActive, Is.EqualTo(usuario.IsActive));
        }

        [Test]
        public void UsuarioMapper_ToDTO_NullUsuario_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => UsuarioMapper.ToDTO(null!));
        }

        [Test]
        public void UsuarioMapper_FromDTO_ValidDTO_ShouldMapCorrectly()
        {
            // Arrange
            var usuarioDto = new UsuarioDTO
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Email = "test@example.com",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            // Act
            var result = UsuarioMapper.FromDTO(usuarioDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(usuarioDto.Id));
            Assert.That(result.Name, Is.EqualTo(usuarioDto.Name));
            Assert.That(result.Email, Is.EqualTo(usuarioDto.Email));
            Assert.That(result.Role, Is.EqualTo(usuarioDto.Role));
            Assert.That(result.IsActive, Is.EqualTo(usuarioDto.IsActive));
        }

        [Test]
        public void UsuarioMapper_FromDTO_NullDTO_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => UsuarioMapper.FromDTO(null!));
        }

        [Test]
        public void UsuarioMapper_ToDTOList_ValidList_ShouldMapCorrectly()
        {
            // Arrange
            var usuarios = new List<Usuario>
            {
                new Usuario(
                    id: Guid.NewGuid(),
                    name: "User 1",
                    email: "user1@example.com",
                    passwordHash: "hashedpassword1",
                    role: "User",
                    isActive: true,
                    activeToken: "token1"
                ),
                new Usuario(
                    id: Guid.NewGuid(),
                    name: "User 2",
                    email: "user2@example.com",
                    passwordHash: "hashedpassword2",
                    role: "Admin",
                    isActive: false,
                    activeToken: "token2"
                )
            };

            // Act
            var result = UsuarioMapper.ToDTOList(usuarios);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            
            var firstDto = result.First();
            Assert.That(firstDto.Name, Is.EqualTo("User 1"));
            Assert.That(firstDto.Email, Is.EqualTo("user1@example.com"));
            Assert.That(firstDto.Role, Is.EqualTo("User"));
            Assert.That(firstDto.IsActive, Is.True);
            
            var secondDto = result.Last();
            Assert.That(secondDto.Name, Is.EqualTo("User 2"));
            Assert.That(secondDto.Email, Is.EqualTo("user2@example.com"));
            Assert.That(secondDto.Role, Is.EqualTo("Admin"));
            Assert.That(secondDto.IsActive, Is.False);
        }

        [Test]
        public void UsuarioMapper_ToDTOList_NullList_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => UsuarioMapper.ToDTOList(null!));
        }

        [Test]
        public void UsuarioMapper_ToDTOList_EmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var usuarios = new List<Usuario>();

            // Act
            var result = UsuarioMapper.ToDTOList(usuarios);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        #endregion
    }
}

