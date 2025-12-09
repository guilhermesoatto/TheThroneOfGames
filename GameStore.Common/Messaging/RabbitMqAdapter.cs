using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Implementação de IEventBus usando RabbitMQ como broker.
    /// Publica eventos para exchanges RabbitMQ com suporte a DLQ (Dead Letter Queue) em caso de falha.
    /// </summary>
    public class RabbitMqAdapter : IEventBus, IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConnection? _connection;
        private readonly IModel? _channel;
        private readonly ILogger<RabbitMqAdapter> _logger;
        private readonly string _exchangeName;
        private readonly string _dlqExchangeName;
        private bool _disposed;

        // Mapeamento de tipos de eventos para filas
        // `typeof(dynamic)` is invalid in C#; use `object` when a generic/any-event mapping is intended.
        private static readonly Dictionary<Type, string> EventQueueMapping = new()
        {
            { typeof(object), "usuario.eventos" },
        };

        public RabbitMqAdapter(
            string host,
            int port,
            string username,
            string password,
            ILogger<RabbitMqAdapter> logger,
            string exchangeName = "thethroneofgames.events",
            string dlqExchangeName = "thethroneofgames.dlq")
        {
            _logger = logger;
            _exchangeName = exchangeName;
            _dlqExchangeName = dlqExchangeName;

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

                InitializeExchangesAndQueues();
                _logger.LogInformation("RabbitMQ adapter initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ adapter");
                throw;
            }
        }

        /// <summary>
        /// Inicializa exchanges, filas e configurações de DLQ no RabbitMQ.
        /// </summary>
        private void InitializeExchangesAndQueues()
        {
            if (_channel == null)
                throw new InvalidOperationException("Channel not initialized");

            // Declarar exchange principal
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            // Declarar exchange DLQ
            _channel.ExchangeDeclare(
                exchange: _dlqExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            // Declarar fila DLQ
            _channel.QueueDeclare(
                queue: "thethroneofgames.dlq.queue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _channel.QueueBind(
                queue: "thethroneofgames.dlq.queue",
                exchange: _dlqExchangeName,
                routingKey: "#"
            );

            _logger.LogInformation("RabbitMQ exchanges and queues initialized");
        }

        /// <summary>
        /// Publica um evento no RabbitMQ.
        /// </summary>
        public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            if (_channel == null)
                throw new InvalidOperationException("Channel not initialized");

            try
            {
                var eventType = typeof(TEvent);
                var routingKey = GetRoutingKey(eventType);
                var messageBody = SerializeEvent(domainEvent);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object>
                {
                    { "x-event-type", eventType.FullName ?? "unknown" },
                    { "x-timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };

                _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(messageBody)
                );

                _logger.LogInformation(
                    "Event published: {EventType} with routing key {RoutingKey}",
                    eventType.Name,
                    routingKey
                );

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event to RabbitMQ");
                throw;
            }
        }

        /// <summary>
        /// Este método não é suportado no adapter RabbitMQ.
        /// Use um consumer/handler separado para processar mensagens.
        /// </summary>
        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
        {
            throw new NotSupportedException(
                "RabbitMQ adapter does not support in-process subscription. " +
                "Use a separate consumer/handler to process messages from the queue."
            );
        }

        /// <summary>
        /// Este método não é suportado no adapter RabbitMQ.
        /// </summary>
        public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
        {
            throw new NotSupportedException(
                "RabbitMQ adapter does not support in-process subscription removal."
            );
        }

        /// <summary>
        /// Este método não é suportado no adapter RabbitMQ.
        /// </summary>
        public int GetHandlerCount<TEvent>() where TEvent : IDomainEvent
        {
            throw new NotSupportedException(
                "RabbitMQ adapter does not track in-process handlers."
            );
        }

        /// <summary>
        /// Obtém a routing key para um tipo de evento.
        /// </summary>
        private string GetRoutingKey(Type eventType)
        {
            var eventName = eventType.Name;
            // Converte UsuarioAtivadoEvent -> usuario.ativado
            var words = System.Text.RegularExpressions.Regex.Matches(eventName, "[A-Z][a-z]*");
            var routingKey = string.Join(".", words.Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value.ToLower()));

            return routingKey;
        }

        /// <summary>
        /// Serializa um evento para JSON.
        /// </summary>
        private string SerializeEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            var json = JsonConvert.SerializeObject(domainEvent, Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            return json;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
                _logger.LogInformation("RabbitMQ adapter disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ adapter");
            }

            _disposed = true;
        }
    }
}
