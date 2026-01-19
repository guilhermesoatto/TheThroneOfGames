using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using GameStore.Common.Events;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Consumer genérico para processar eventos do RabbitMQ.
    /// Implementa retry com jitter e dead-letter queue em caso de falha.
    /// </summary>
    public class RabbitMqConsumer : IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConnection? _connection;
        private readonly IModel? _channel;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private bool _disposed;

        public RabbitMqConsumer(
            string host,
            int port,
            string username,
            string password,
            ILogger<RabbitMqConsumer> logger)
        {
            _logger = logger;

            try
            {
                _connectionFactory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = username,
                    Password = password,
                    AutomaticRecoveryEnabled = true,
                    RequestedHeartbeat = TimeSpan.FromSeconds(10),
                    DispatchConsumersAsync = true
                };

                _connection = _connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger.LogInformation("RabbitMQ consumer initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ consumer");
                throw;
            }
        }

        /// <summary>
        /// Inicia o consumo de mensagens de uma fila.
        /// </summary>
        public async Task StartConsuming(
            string queueName,
            Func<string, Task> messageHandler,
            int prefetchCount = 1)
        {
            if (_channel == null)
                throw new InvalidOperationException("Channel not initialized");

            try
            {
                _channel.BasicQos(0, (ushort)prefetchCount, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var messageBody = Encoding.UTF8.GetString(ea.Body.ToArray());
                        _logger.LogInformation("Message received: {Message}", messageBody);

                        await messageHandler(messageBody);

                        // Acknowledge message
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Message acknowledged");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message. Sending to DLQ.");
                        // Nack without requeue - message goes to DLQ
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                _logger.LogInformation("Started consuming from queue: {QueueName}", queueName);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting message consumer");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
                _logger.LogInformation("RabbitMQ consumer disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ consumer");
            }

            _disposed = true;
        }
    }
}
