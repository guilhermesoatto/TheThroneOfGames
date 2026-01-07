using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Serviço para gerenciar consumers de eventos RabbitMQ.
    /// Inicia e para todos os consumers quando a aplicação inicia/para.
    /// </summary>
    public class EventConsumerService : IHostedService, IDisposable
    {
        private readonly ILogger<EventConsumerService> _logger;
        private readonly IEnumerable<IEventConsumer> _consumers;
        private bool _disposed;

        public EventConsumerService(
            ILogger<EventConsumerService> logger,
            IEnumerable<IEventConsumer> consumers)
        {
            _logger = logger;
            _consumers = consumers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting event consumers...");

            var startTasks = new List<Task>();
            foreach (var consumer in _consumers)
            {
                startTasks.Add(consumer.StartConsumingAsync());
            }

            await Task.WhenAll(startTasks);
            _logger.LogInformation("All event consumers started successfully");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping event consumers...");

            var stopTasks = new List<Task>();
            foreach (var consumer in _consumers)
            {
                stopTasks.Add(consumer.StopConsumingAsync());
            }

            await Task.WhenAll(stopTasks);
            _logger.LogInformation("All event consumers stopped");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var consumer in _consumers)
            {
                if (consumer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _disposed = true;
        }
    }
}