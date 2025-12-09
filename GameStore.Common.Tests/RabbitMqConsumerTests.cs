using GameStore.Common.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace GameStore.Common.Tests
{
    /// <summary>
    /// Testes para o RabbitMQ consumer.
    /// Valida inicialização e processamento de mensagens.
    /// </summary>
    [TestFixture]
    public class RabbitMqConsumerTests
    {
        private Mock<ILogger<RabbitMqConsumer>> _mockLogger = null!;
        private RabbitMqConsumer _consumer = null!;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<RabbitMqConsumer>>();
        }

        [TearDown]
        public void TearDown()
        {
            _consumer?.Dispose();
        }

        [Test]
        public void RabbitMqConsumer_Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            _consumer = new RabbitMqConsumer(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            // Assert
            Assert.IsNotNull(_consumer);
        }

        [Test]
        public void RabbitMqConsumer_Constructor_WithInvalidHost_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<Exception>(() =>
                new RabbitMqConsumer(
                    host: "invalid-host-xyz",
                    port: 5672,
                    username: "guest",
                    password: "guest",
                    logger: _mockLogger.Object
                )
            );
        }

        [Test]
        public async Task RabbitMqConsumer_StartConsuming_WithValidQueue_StartsSuccessfully()
        {
            // Arrange
            _consumer = new RabbitMqConsumer(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            bool messageHandlerCalled = false;
            Func<string, Task> messageHandler = async (message) =>
            {
                messageHandlerCalled = true;
                await Task.CompletedTask;
            };

            // Act
            await _consumer.StartConsuming("test.queue", messageHandler);

            // Assert - verify logging
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
        public void RabbitMqConsumer_Dispose_DisposeResourcesSuccessfully()
        {
            // Arrange
            _consumer = new RabbitMqConsumer(
                host: "localhost",
                port: 5672,
                username: "guest",
                password: "guest",
                logger: _mockLogger.Object
            );

            // Act
            _consumer.Dispose();

            // Assert - second dispose should not throw
            Assert.DoesNotThrow(() => _consumer.Dispose());
        }
    }
}
