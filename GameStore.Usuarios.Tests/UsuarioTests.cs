using NUnit.Framework;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using System;

namespace GameStore.Usuarios.Tests
{
    public class UsuarioTests
    {
        [Test]
        public void Usuario_Activation_Toggles_IsActive()
        {
            // Arrange
            var name = "Test User";
            var email = "test@example.com";
            var passwordHash = "hash";
            var role = "User";
            var token = Guid.NewGuid().ToString();

            var usuario = new Usuario(name, email, passwordHash, role, token);

            // Assert initial state
            Assert.IsFalse(usuario.IsActive, "Usuario should be inactive after creation");

            // Act
            usuario.Activate();

            // Assert
            Assert.IsTrue(usuario.IsActive, "Usuario should be active after Activate()");
        }

        [Test]
        public void Usuario_UpdateProfile_Changes_Name_And_Email()
        {
            // Arrange
            var usuario = new Usuario("Old", "old@example.com", "hash", "User", Guid.NewGuid().ToString());

            // Act
            usuario.UpdateProfile("New Name", "new@example.com");

            // Assert
            Assert.AreEqual("New Name", usuario.Name);
            Assert.AreEqual("new@example.com", usuario.Email);
        }

        [Test]
        public void Usuario_UpdateRole_Changes_Role_Successfully()
        {
            // Arrange
            var usuario = new Usuario("Test User", "test@example.com", "hash", "User", Guid.NewGuid().ToString());
            
            // Act
            usuario.UpdateRole("Admin");

            // Assert
            Assert.AreEqual("Admin", usuario.Role);
        }

        [Test]
        public void Usuario_UpdateRole_Throws_Exception_With_Empty_Role()
        {
            // Arrange
            var usuario = new Usuario("Test User", "test@example.com", "hash", "User", Guid.NewGuid().ToString());

            // Act & Assert
            Assert.Throws<ArgumentException>(() => usuario.UpdateRole(""));
        }

        [Test]
        public void Usuario_Disable_Sets_IsActive_False()
        {
            // Arrange
            var usuario = new Usuario("Test User", "test@example.com", "hash", "User", Guid.NewGuid().ToString());
            usuario.Activate();
            Assert.IsTrue(usuario.IsActive);

            // Act
            usuario.Disable();

            // Assert
            Assert.IsFalse(usuario.IsActive);
        }

        [Test]
        public void Usuario_Enable_Sets_IsActive_True()
        {
            // Arrange
            var usuario = new Usuario("Test User", "test@example.com", "hash", "User", Guid.NewGuid().ToString());
            usuario.Disable();
            Assert.IsFalse(usuario.IsActive);

            // Act
            usuario.Enable();

            // Assert
            Assert.IsTrue(usuario.IsActive);
        }

        [Test]
        public void UsuarioMapper_ToDTO_Converts_Usuario_To_DTO()
        {
            // Arrange
            var usuario = new Usuario("John Doe", "john@example.com", "hash123", "Admin", "token123");
            usuario.Activate();

            // Act
            var dto = UsuarioMapper.ToDTO(usuario);

            // Assert
            Assert.AreEqual(usuario.Id, dto.Id);
            Assert.AreEqual("John Doe", dto.Name);
            Assert.AreEqual("john@example.com", dto.Email);
            Assert.AreEqual("Admin", dto.Role);
            Assert.IsTrue(dto.IsActive);
        }

        [Test]
        public void UsuarioMapper_FromDTO_Converts_DTO_To_Usuario()
        {
            // Arrange
            var dto = new UsuarioDTO
            {
                Id = Guid.NewGuid(),
                Name = "Jane Doe",
                Email = "jane@example.com",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var usuario = UsuarioMapper.FromDTO(dto);

            // Assert
            Assert.AreEqual(dto.Id, usuario.Id);
            Assert.AreEqual("Jane Doe", usuario.Name);
            Assert.AreEqual("jane@example.com", usuario.Email);
            Assert.AreEqual("User", usuario.Role);
        }

        [Test]
        public void UsuarioMapper_ToDTO_Throws_With_Null_Usuario()
        {
            // Act & Assert
            Usuario? nullUser = null;
            Assert.Throws<ArgumentNullException>(() => UsuarioMapper.ToDTO(nullUser!));
        }

        [Test]
        public void UsuarioMapper_FromDTO_Throws_With_Null_DTO()
        {
            // Act & Assert
            UsuarioDTO? nullDto = null;
            Assert.Throws<ArgumentNullException>(() => UsuarioMapper.FromDTO(nullDto!));
        }

        [Test]
        public void UsuarioMapper_ToDTOList_Converts_Multiple_Usuarios()
        {
            // Arrange
            var usuarios = new[]
            {
                new Usuario("User 1", "user1@example.com", "hash", "User", "token1"),
                new Usuario("User 2", "user2@example.com", "hash", "User", "token2"),
                new Usuario("User 3", "user3@example.com", "hash", "Admin", "token3")
            };

            // Act
            var dtos = UsuarioMapper.ToDTOList(usuarios).ToList();

            // Assert
            Assert.AreEqual(3, dtos.Count);
            Assert.AreEqual("User 1", dtos[0].Name);
            Assert.AreEqual("User 3", dtos[2].Name);
            Assert.AreEqual("Admin", dtos[2].Role);
        }

        [Test]
        public void Usuario_Constructor_Initializes_With_Generated_Id()
        {
            // Arrange & Act
            var usuario = new Usuario("Test", "test@example.com", "hash", "User", "token");

            // Assert
            Assert.AreNotEqual(Guid.Empty, usuario.Id);
            Assert.IsNotNull(usuario.Id);
        }

        [Test]
        public void Usuario_Constructor_With_Id_Preserves_Guid()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var usuario = new Usuario(userId, "Test", "test@example.com", "hash", "User", true, "token");

            // Assert
            Assert.AreEqual(userId, usuario.Id);
        }

        [Test]
        public void Usuario_UpdateProfile_Throws_With_Empty_Name()
        {
            // Arrange
            var usuario = new Usuario("Test", "test@example.com", "hash", "User", "token");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => usuario.UpdateProfile("", "newemail@example.com"));
        }

        [Test]
        public void Usuario_UpdateProfile_Throws_With_Empty_Email()
        {
            // Arrange
            var usuario = new Usuario("Test", "test@example.com", "hash", "User", "token");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => usuario.UpdateProfile("New Name", ""));
        }
    }
}
