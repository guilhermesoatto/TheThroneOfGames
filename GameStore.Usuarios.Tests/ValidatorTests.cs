using NUnit.Framework;
using GameStore.Usuarios.Application.Commands;
using GameStore.Usuarios.Application.Validators;
using GameStore.CQRS.Abstractions;

namespace GameStore.Usuarios.Tests
{
    [TestFixture]
    public class ValidatorTests
    {
        #region CreateUserCommandValidator Tests

        [Test]
        public void CreateUserCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new CreateUserCommandValidator();
            var command = new CreateUserCommand("John Doe", "john.doe@example.com", "StrongPassword123!", "User");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void CreateUserCommandValidator_EmptyName_ShouldFail()
        {
            // Arrange
            var validator = new CreateUserCommandValidator();
            var command = new CreateUserCommand("", "john.doe@example.com", "StrongPassword123!", "User");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "Name"), Is.True);
        }

        [Test]
        public void CreateUserCommandValidator_InvalidEmail_ShouldFail()
        {
            // Arrange
            var validator = new CreateUserCommandValidator();
            var command = new CreateUserCommand("John Doe", "invalid-email", "StrongPassword123!", "User");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "Email"), Is.True);
        }

        [Test]
        public void CreateUserCommandValidator_WeakPassword_ShouldFail()
        {
            // Arrange
            var validator = new CreateUserCommandValidator();
            var command = new CreateUserCommand("John Doe", "john.doe@example.com", "123", "User");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "Password"), Is.True);
        }

        [Test]
        public void CreateUserCommandValidator_EmptyRole_ShouldFail()
        {
            // Arrange
            var validator = new CreateUserCommandValidator();
            var command = new CreateUserCommand("John Doe", "john.doe@example.com", "StrongPassword123!", "");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "Role"), Is.True);
        }

        #endregion

        #region UpdateUserProfileCommandValidator Tests

        [Test]
        public void UpdateUserProfileCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new UpdateUserProfileCommandValidator();
            var command = new UpdateUserProfileCommand("john.doe@example.com", "John Doe", "john.doe@example.com");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void UpdateUserProfileCommandValidator_EmptyUserId_ShouldFail()
        {
            // Arrange
            var validator = new UpdateUserProfileCommandValidator();
            var command = new UpdateUserProfileCommand("", "John Doe", "john.doe@example.com");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "ExistingEmail"), Is.True);
        }

        [Test]
        public void UpdateUserProfileCommandValidator_EmptyName_ShouldFail()
        {
            // Arrange
            var validator = new UpdateUserProfileCommandValidator();
            var command = new UpdateUserProfileCommand("john.doe@example.com", "", "john.doe@example.com");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "NewName"), Is.True);
        }

        #endregion

        #region ActivateUserCommandValidator Tests

        [Test]
        public void ActivateUserCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new ActivateUserCommandValidator();
            var command = new ActivateUserCommand(Guid.NewGuid().ToString());

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ActivateUserCommandValidator_EmptyUserId_ShouldFail()
        {
            // Arrange
            var validator = new ActivateUserCommandValidator();
            var command = new ActivateUserCommand("");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "ActivationToken"), Is.True);
        }

        #endregion

        #region ChangeUserRoleCommandValidator Tests

        [Test]
        public void ChangeUserRoleCommandValidator_ValidCommand_ShouldPass()
        {
            // Arrange
            var validator = new ChangeUserRoleCommandValidator();
            var command = new ChangeUserRoleCommand("user@example.com", "Admin");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ChangeUserRoleCommandValidator_EmptyUserId_ShouldFail()
        {
            // Arrange
            var validator = new ChangeUserRoleCommandValidator();
            var command = new ChangeUserRoleCommand("", "Admin");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "Email"), Is.True);
        }

        [Test]
        public void ChangeUserRoleCommandValidator_EmptyRole_ShouldFail()
        {
            // Arrange
            var validator = new ChangeUserRoleCommandValidator();
            var command = new ChangeUserRoleCommand("user@example.com", "");

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.PropertyName == "NewRole"), Is.True);
        }

        #endregion
    }
}
