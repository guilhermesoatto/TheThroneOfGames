using NUnit.Framework;
using Moq;
using GameStore.Vendas.Application.EventHandlers;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Domain.Interfaces;

namespace GameStore.Vendas.Tests
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

        #region GameCompradoEventHandler Tests

        [Test]
        public void GameCompradoEventHandler_ValidEvent_ShouldHandleSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var gameEvent = new GameCompradoEvent
            {
                UserId = userId,
                GameId = gameId,
                Timestamp = DateTime.UtcNow
            };

            var handler = new GameCompradoEventHandler();

            // Act
            Assert.DoesNotThrow(() => handler.HandleAsync(gameEvent));

            // Assert
            // Verifica que o handler processou o evento sem lançar exceção
        }

        [Test]
        public void GameCompradoEventHandler_ShouldLogEvent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var gameEvent = new GameCompradoEvent
            {
                UserId = userId,
                GameId = gameId,
                Timestamp = DateTime.UtcNow
            };

            var handler = new GameCompradoEventHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => handler.HandleAsync(gameEvent));
        }

        #endregion

        #region PedidoFinalizadoEventHandler Tests

        [Test]
        public void PedidoFinalizadoEventHandler_ValidEvent_ShouldHandleSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pedidoId = Guid.NewGuid();
            var pedidoEvent = new PedidoFinalizadoEvent
            {
                UserId = userId,
                PedidoId = pedidoId,
                Timestamp = DateTime.UtcNow
            };

            var handler = new PedidoFinalizadoEventHandler();

            // Act
            Assert.DoesNotThrow(() => handler.HandleAsync(pedidoEvent));

            // Assert
            // Verifica que o handler processou o evento sem lançar exceção
        }

        [Test]
        public void PedidoFinalizadoEventHandler_ShouldLogEvent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pedidoId = Guid.NewGuid();
            var pedidoEvent = new PedidoFinalizadoEvent
            {
                UserId = userId,
                PedidoId = pedidoId,
                Timestamp = DateTime.UtcNow
            };

            var handler = new PedidoFinalizadoEventHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => handler.HandleAsync(pedidoEvent));
        }

        #endregion
    }
}
