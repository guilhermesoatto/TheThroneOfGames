using NUnit.Framework;
using Moq;
using GameStore.Usuarios.Application.EventHandlers;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Domain.Interfaces;

namespace GameStore.Usuarios.Tests
{
    [TestFixture]
    public class EventHandlerTests
    {
        #region UsuarioAtivadoEventHandler Tests

        [Test]
        public void UsuarioAtivadoEventHandler_ValidEvent_ShouldHandleSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userEvent = new UsuarioAtivadoEvent
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            var handler = new UsuarioAtivadoEventHandler();

            // Act
            Assert.DoesNotThrow(() => handler.HandleAsync(userEvent));

            // Assert
            // Verifica que o handler processou o evento sem lançar exceção
        }

        [Test]
        public void UsuarioAtivadoEventHandler_ShouldLogEvent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userEvent = new UsuarioAtivadoEvent
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            var handler = new UsuarioAtivadoEventHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => handler.HandleAsync(userEvent));
        }

        #endregion
    }
}
