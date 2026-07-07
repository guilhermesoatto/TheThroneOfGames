using GameStore.Common.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

namespace GameStore.Common.Tests
{
    /// <summary>
    /// Testes para o RabbitMQ consumer.
    /// Valida inicialização e processamento de mensagens.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class RabbitMqConsumerTests
    {
        private Mock<ILogger<RabbitMqConsumer>> _mockLogger = null!;
        private RabbitMqConsumer _consumer = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Limpar todas as filas do RabbitMQ antes dos testes
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest"
                };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                
                // Tentar deletar filas que podem existir
                try { channel.QueueDelete("test.queue"); } catch { }
                try { channel.QueueDelete("catalogo.usuario-ativado"); } catch { }
                try { channel.QueueDelete("catalogo.usuario-ativado.dlq"); } catch { }
            }
            catch
            {
                // Ignora se RabbitMQ não está disponível
            }
        }

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
            // Act & Assert - Assert.Catch (não Assert.Throws) porque a exceção real lançada
            // é RabbitMQ.Client.Exceptions.BrokerUnreachableException, uma subclasse de
            // Exception; Assert.Throws<Exception> no NUnit exige o tipo exato.
            Assert.Catch<Exception>(() =>
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
            // Arrange - BasicConsume exige que a fila já exista num broker real
            var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare("test.queue", durable: false, exclusive: false, autoDelete: false);
            }

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
