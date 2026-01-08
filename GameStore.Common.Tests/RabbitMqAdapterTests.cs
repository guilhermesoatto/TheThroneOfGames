using GameStore.Common.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using GameStore.Common.Events;

namespace GameStore.Common.Tests
{
    /// <summary>
    /// Testes para o RabbitMQ adapter.
    /// Valida comportamento de publicação, tratamento de erros e logging.
    /// </summary>
    [TestFixture]
    public class RabbitMqAdapterTests
    {
        private Mock<ILogger<RabbitMqAdapter>> _mockLogger = null!;
        private RabbitMqAdapter _adapter = null!;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<RabbitMqAdapter>>();
        }

        [TearDown]
        public void TearDown()
        {
            _adapter?.Dispose();
        }

        [Test]
        public void RabbitMqAdapter_Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object,
                exchangeName: "test.exchange",
                dlqExchangeName: "test.dlq"
            );

            // Assert - constructor completed without throwing
            Assert.IsNotNull(_adapter);
        }

        [Test]
        public void RabbitMqAdapter_Constructor_WithInvalidHost_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<Exception>(() =>
                new RabbitMqAdapter(
                    host: "invalid-host-xyz",
                    port: 5672,
                    username: "guest",
                    password: "guest",
                    logger: _mockLogger.Object
                )
            );
        }

        [Test]
        public async Task RabbitMqAdapter_PublishAsync_WithValidEvent_LogsSuccess()
        {
            // Arrange
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            var testEvent = new UsuarioAtivadoEvent(
                UsuarioId: Guid.NewGuid(),
                Email: "test@example.com",
                Nome: "Test User"
            );

            // Act
            await _adapter.PublishAsync(testEvent);

            // Assert - verify logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Test]
        public void RabbitMqAdapter_PublishAsync_WithNullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _adapter.PublishAsync<UsuarioAtivadoEvent>(null!)
            );
        }

        [Test]
        public void RabbitMqAdapter_Subscribe_ThrowsNotSupportedException()
        {
            // Arrange
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            var mockHandler = new Mock<IEventHandler<UsuarioAtivadoEvent>>();

            // Act & Assert
            Assert.Throws<NotSupportedException>(
                () => _adapter.Subscribe(mockHandler.Object)
            );
        }

        [Test]
        public void RabbitMqAdapter_Unsubscribe_ThrowsNotSupportedException()
        {
            // Arrange
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            var mockHandler = new Mock<IEventHandler<UsuarioAtivadoEvent>>();

            // Act & Assert
            Assert.Throws<NotSupportedException>(
                () => _adapter.Unsubscribe(mockHandler.Object)
            );
        }

        [Test]
        public void RabbitMqAdapter_GetHandlerCount_ThrowsNotSupportedException()
        {
            // Arrange
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            // Act & Assert
            Assert.Throws<NotSupportedException>(
                () => _adapter.GetHandlerCount<UsuarioAtivadoEvent>()
            );
        }

        [Test]
        public void RabbitMqAdapter_Dispose_DisposeResourcesSuccessfully()
        {
            // Arrange
            _adapter = new RabbitMqAdapter(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            // Act
            _adapter.Dispose();

            // Assert - second dispose should not throw
            Assert.DoesNotThrow(() => _adapter.Dispose());

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }
    }
}
