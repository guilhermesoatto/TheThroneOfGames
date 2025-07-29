using Moq;
using NUnit.Framework;
using TheThroneOfGames.Application;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;

namespace Test.Application
{
    [TestFixture]
    public class UsuarioServiceUnitTest
    {
        private Mock<IUsuarioRepository> _userRepositoryMock;
        private UsuarioService _usuarioService;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUsuarioRepository>();
            _usuarioService = new UsuarioService(_userRepositoryMock.Object);
        }

        [Test]
        public async Task PreRegisterUserAsync_ValidInputs_ShouldCallAddAsync()
        {
            // Arrange
            string email = "test@test.com";
            string name = "Test User";

            // Act
            await _usuarioService.PreRegisterUserAsync(email, name);

            // Assert
            _userRepositoryMock.Verify(x => x.AddAsync(It.Is<Usuario>(u =>
                u.Email == email &&
                u.Name == name &&
                !u.IsActive &&
                u.Role == "User")), Times.Once);
        }

        [Test]
        public void PreRegisterUserAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            string email = "test@test.com";
            string name = "Test User";
            _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Usuario>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => 
                await _usuarioService.PreRegisterUserAsync(email, name));
        }

        [Test]
        public async Task ActivateUserAsync_WhenTokenIsValid_ShouldActivateUser()
        {
            // Arrange
            var activationToken = "validToken";
            var user = new Usuario(
                "Test User",
                "test@test.com",
                "hashSenha",
                "User",
                activationToken
            );
            
            _userRepositoryMock.Setup(x => x.GetByActivationTokenAsync(activationToken))
                .ReturnsAsync(user);

            // Act
            await _usuarioService.ActivateUserAsync(activationToken);

            // Assert
            Assert.That(user.IsActive, Is.True);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Test]
        public void ActivateUserAsync_WhenTokenIsInvalid_ShouldThrowException()
        {
            // Arrange
            var invalidToken = "invalidToken";
            _userRepositoryMock.Setup(x => x.GetByActivationTokenAsync(invalidToken))
                .ReturnsAsync((Usuario)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => 
                await _usuarioService.ActivateUserAsync(invalidToken));
            Assert.That(ex.Message, Is.EqualTo("Token inválido ou expirado."));
        }
    }
}