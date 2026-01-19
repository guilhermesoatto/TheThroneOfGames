using NUnit.Framework;
using Moq;
using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.Handlers;
using GameStore.Usuarios.Application.Validators;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Usuarios.Application.Mappers;
using GameStore.Usuarios.Domain.Entities;
using GameStore.Usuarios.Domain.Interfaces;
using GameStore.Common.Events;
using GameStore.Common.Messaging;

namespace GameStore.Usuarios.Tests
{
    [TestFixture]
    public class CommandHandlerTests
    {
        private Mock<IUsuarioRepository> _mockUsuarioRepository = null!;
        private Mock<IEventBus> _mockEventBus = null!;

        [SetUp]
        public void Setup()
        {
            _mockUsuarioRepository = new Mock<IUsuarioRepository>();
            _mockEventBus = new Mock<IEventBus>();
        }

        #region ActivateUserCommandHandler Tests

        [Test]
        public async Task ActivateUserCommandHandler_ValidToken_ShouldActivateUserAndPublishEvent()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid();
            var user = new Usuario(userId, "Test User", "test@example.com", "hashed", "User", false, token);

            var command = new ActivateUserCommand(token);
            var handler = new ActivateUserCommandHandler(_mockUsuarioRepository.Object, _mockEventBus.Object);

            _mockUsuarioRepository.Setup(r => r.GetByActivationTokenAsync(token))
                .ReturnsAsync(user);
            _mockUsuarioRepository.Setup(r => r.UpdateAsync(user))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Usuário ativado com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(userId));
            Assert.That(user.IsActive, Is.True);

            _mockEventBus.Verify(e => e.PublishAsync(It.Is<UsuarioAtivadoEvent>(
                evt => evt.UsuarioId == userId && evt.Email == user.Email)), Times.Once);
        }

        [Test]
        public async Task ActivateUserCommandHandler_InvalidToken_ShouldReturnError()
        {
            // Arrange
            var token = "invalid-token";
            var command = new ActivateUserCommand(token);
            var handler = new ActivateUserCommandHandler(_mockUsuarioRepository.Object, _mockEventBus.Object);

            _mockUsuarioRepository.Setup(r => r.GetByActivationTokenAsync(token))
                .ReturnsAsync((Usuario?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Token inválido ou expirado"));
            Assert.That(result.Errors, Contains.Item("Token não encontrado ou já utilizado"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<UsuarioAtivadoEvent>()), Times.Never);
        }

        [Test]
        public async Task ActivateUserCommandHandler_EmptyToken_ShouldFailValidation()
        {
            // Arrange
            var command = new ActivateUserCommand("");
            var handler = new ActivateUserCommandHandler(_mockUsuarioRepository.Object, _mockEventBus.Object);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Validação falhou"));
            Assert.That(result.Errors, Contains.Item("Token de ativação é obrigatório."));

            _mockUsuarioRepository.Verify(r => r.GetByActivationTokenAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region UpdateUserProfileCommandHandler Tests

        [Test]
        public async Task UpdateUserProfileCommandHandler_ValidData_ShouldUpdateProfileAndPublishEvent()
        {
            // Arrange
            var existingEmail = "old@example.com";
            var newEmail = "new@example.com";
            var newName = "New Name";
            var userId = Guid.NewGuid();
            var user = new Usuario(userId, "Old Name", existingEmail, "hashed", "User", true, "");

            var command = new UpdateUserProfileCommand(existingEmail, newName, newEmail);
            var handler = new UpdateUserProfileCommandHandler(_mockUsuarioRepository.Object, _mockEventBus.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(existingEmail))
                .ReturnsAsync(user);
            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(newEmail))
                .ReturnsAsync((Usuario?)null); // Novo email não existe
            _mockUsuarioRepository.Setup(r => r.UpdateAsync(user))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Perfil atualizado com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(userId));
            Assert.That(user.Name, Is.EqualTo(newName));
            Assert.That(user.Email, Is.EqualTo(newEmail));

            _mockEventBus.Verify(e => e.PublishAsync(It.Is<UsuarioPerfillAtualizadoEvent>(
                evt => evt.UsuarioId == userId && evt.NovoNome == newName && evt.NovoEmail == newEmail)), Times.Once);
        }

        [Test]
        public async Task UpdateUserProfileCommandHandler_EmailAlreadyExists_ShouldReturnError()
        {
            // Arrange
            var existingEmail = "old@example.com";
            var newEmail = "existing@example.com";
            var userId = Guid.NewGuid();
            var user = new Usuario(userId, "Old Name", existingEmail, "hashed", "User", true, "");
            var existingUser = new Usuario(Guid.NewGuid(), "Other User", newEmail, "hashed", "User", true, "");

            var command = new UpdateUserProfileCommand(existingEmail, "New Name", newEmail);
            var handler = new UpdateUserProfileCommandHandler(_mockUsuarioRepository.Object, _mockEventBus.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(existingEmail))
                .ReturnsAsync(user);
            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(newEmail))
                .ReturnsAsync(existingUser);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Email já está em uso"));
            Assert.That(result.Errors, Contains.Item($"O email {newEmail} já está cadastrado"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<UsuarioPerfillAtualizadoEvent>()), Times.Never);
        }

        [Test]
        public async Task UpdateUserProfileCommandHandler_UserNotFound_ShouldReturnError()
        {
            // Arrange
            var existingEmail = "nonexistent@example.com";
            var command = new UpdateUserProfileCommand(existingEmail, "New Name", "new@example.com");
            var handler = new UpdateUserProfileCommandHandler(_mockUsuarioRepository.Object, _mockEventBus.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(existingEmail))
                .ReturnsAsync((Usuario?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Usuário não encontrado"));
            Assert.That(result.Errors, Contains.Item($"Usuário com email {existingEmail} não encontrado"));

            _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<UsuarioPerfillAtualizadoEvent>()), Times.Never);
        }

        #endregion

        #region CreateUserCommandHandler Tests

        [Test]
        public async Task CreateUserCommandHandler_ValidData_ShouldCreateUser()
        {
            // Arrange
            var command = new CreateUserCommand("New User", "new@example.com", "Password123", "User");
            var handler = new CreateUserCommandHandler(_mockUsuarioRepository.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync("new@example.com"))
                .ReturnsAsync((Usuario?)null);
            _mockUsuarioRepository.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Usuário criado com sucesso"));
            Assert.That(result.EntityId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(((UsuarioDTO)result.Data!).Name, Is.EqualTo("New User"));

            _mockUsuarioRepository.Verify(r => r.AddAsync(It.Is<Usuario>(
                u => u.Name == "New User" && u.Email == "new@example.com" && !u.IsActive)), Times.Once);
        }

        [Test]
        public async Task CreateUserCommandHandler_EmailAlreadyExists_ShouldReturnError()
        {
            // Arrange
            var existingUser = new Usuario(Guid.NewGuid(), "Existing User", "existing@example.com", "hashed", "User", true, "");
            var command = new CreateUserCommand("New User", "existing@example.com", "Password123", "User");
            var handler = new CreateUserCommandHandler(_mockUsuarioRepository.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync("existing@example.com"))
                .ReturnsAsync(existingUser);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Email já está em uso"));
            Assert.That(result.Errors, Contains.Item($"O email existing@example.com já está cadastrado"));

            _mockUsuarioRepository.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Test]
        public async Task CreateUserCommandHandler_WeakPassword_ShouldFailValidation()
        {
            // Arrange
            var command = new CreateUserCommand("New User", "new@example.com", "weak", "User");
            var handler = new CreateUserCommandHandler(_mockUsuarioRepository.Object);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Validação falhou"));
            Assert.That(result.Errors.Count, Is.GreaterThan(0)); // Deve ter múltiplos erros de senha

            _mockUsuarioRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region ChangeUserRoleCommandHandler Tests

        [Test]
        public async Task ChangeUserRoleCommandHandler_ValidData_ShouldChangeRole()
        {
            // Arrange
            var email = "user@example.com";
            var newRole = "Admin";
            var userId = Guid.NewGuid();
            var user = new Usuario(userId, "Test User", email, "hashed", "User", true, "");

            var command = new ChangeUserRoleCommand(email, newRole);
            var handler = new ChangeUserRoleCommandHandler(_mockUsuarioRepository.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(user);
            _mockUsuarioRepository.Setup(r => r.UpdateAsync(user))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Role alterada com sucesso"));
            Assert.That(result.EntityId, Is.EqualTo(userId));
            Assert.That(user.Role, Is.EqualTo(newRole));

            _mockUsuarioRepository.Verify(r => r.UpdateAsync(It.Is<Usuario>(u => u.Role == newRole)), Times.Once);
        }

        [Test]
        public async Task ChangeUserRoleCommandHandler_UserNotFound_ShouldReturnError()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var command = new ChangeUserRoleCommand(email, "Admin");
            var handler = new ChangeUserRoleCommandHandler(_mockUsuarioRepository.Object);

            _mockUsuarioRepository.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync((Usuario?)null);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Usuário não encontrado"));
            Assert.That(result.Errors, Contains.Item($"Usuário com email {email} não encontrado"));

            _mockUsuarioRepository.Verify(r => r.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
        }

        #endregion

        #region Command Validation Tests

        [Test]
        public void UsuarioValidators_ActivateUserCommand_ValidToken_ShouldPass()
        {
            // Arrange
            var command = new ActivateUserCommand(Guid.NewGuid().ToString());

            // Act
            var result = UsuarioValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void UsuarioValidators_ActivateUserCommand_EmptyToken_ShouldFail()
        {
            // Arrange
            var command = new ActivateUserCommand("");

            // Act
            var result = UsuarioValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.ErrorMessage == "Token de ativação é obrigatório."), Is.True);
        }

        [Test]
        public void UsuarioValidators_CreateUserCommand_ValidData_ShouldPass()
        {
            // Arrange
            var command = new CreateUserCommand("Valid Name", "valid@example.com", "ValidPass123", "User");

            // Act
            var result = UsuarioValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void UsuarioValidators_CreateUserCommand_InvalidEmail_ShouldFail()
        {
            // Arrange
            var command = new CreateUserCommand("Valid Name", "invalid-email", "ValidPass123", "User");

            // Act
            var result = UsuarioValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.ErrorMessage == "Email inválido."), Is.True);
        }

        [Test]
        public void UsuarioValidators_CreateUserCommand_WeakPassword_ShouldFail()
        {
            // Arrange
            var command = new CreateUserCommand("Valid Name", "valid@example.com", "weak", "User");

            // Act
            var result = UsuarioValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0)); // Expect at least one password error
        }

        [Test]
        public void UsuarioValidators_CreateUserCommand_InvalidRole_ShouldFail()
        {
            // Arrange
            var command = new CreateUserCommand("Valid Name", "valid@example.com", "ValidPass123", "InvalidRole");

            // Act
            var result = UsuarioValidators.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.ErrorMessage == "Role deve ser 'User' ou 'Admin'."), Is.True);
        }

        #endregion
    }
}

