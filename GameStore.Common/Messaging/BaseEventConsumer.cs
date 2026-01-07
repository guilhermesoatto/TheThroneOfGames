using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Classe base para consumers de eventos de domínio via RabbitMQ.
    /// </summary>
    /// <typeparam name="TEvent">Tipo do evento a ser consumido</typeparam>
    public abstract class BaseEventConsumer<TEvent> : IEventConsumer<TEvent>, IDisposable where TEvent : IDomainEvent
    {
        protected readonly RabbitMqConsumer _consumer;
        protected readonly ILogger _logger;
        private readonly string _queueName;
        private bool _isConsuming;
        private bool _disposed;

        protected BaseEventConsumer(
            string host,
            int port,
            string username,
            string password,
            ILogger logger,
            string queueName)
        {
            _logger = logger;
            _queueName = queueName;
            
            try
            {
                // Criar RabbitMqConsumer sem logger genérico
                var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    UserName = username,
                    Password = password,
                    AutomaticRecoveryEnabled = true,
                    RequestedHeartbeat = TimeSpan.FromSeconds(10),
                    DispatchConsumersAsync = true
                };

                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                
                _consumer = new RabbitMqConsumer(host, port, username, password, 
                    new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<RabbitMqConsumer>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ consumer");
                throw;
            }
        }

        public async Task StartConsumingAsync()
        {
            if (_isConsuming)
            {
                _logger.LogWarning("Consumer for {EventType} is already consuming", typeof(TEvent).Name);
                return;
            }

            _logger.LogInformation("Starting consumer for {EventType} on queue {QueueName}",
                typeof(TEvent).Name, _queueName);

            await _consumer.StartConsuming(_queueName, ProcessMessageAsync);
            _isConsuming = true;
        }

        public async Task StopConsumingAsync()
        {
            _isConsuming = false;
            _logger.LogInformation("Stopped consumer for {EventType}", typeof(TEvent).Name);
            await Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(string messageBody)
        {
            try
            {
                var domainEvent = JsonConvert.DeserializeObject<TEvent>(messageBody);
                if (domainEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize message to {EventType}", typeof(TEvent).Name);
                    return;
                }

                await ProcessEventAsync(domainEvent);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing message to {EventType}", typeof(TEvent).Name);
                throw; // Re-throw to send to DLQ
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType}", typeof(TEvent).Name);
                throw; // Re-throw to send to DLQ
            }
        }

        public abstract Task ProcessEventAsync(TEvent domainEvent);

        public void Dispose()
        {
            if (_disposed)
                return;

            _consumer?.Dispose();
            _disposed = true;
        }
    }
}