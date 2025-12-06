using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Extensões para registrar o RabbitMQ adapter no contêiner de DI.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registra o RabbitMQ adapter como implementação de IEventBus.
        /// </summary>
        public static IServiceCollection AddRabbitMqEventBus(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var rabbitMqConfig = configuration.GetSection("RabbitMq");
            var host = rabbitMqConfig.GetValue<string>("Host") ?? "localhost";
            var port = rabbitMqConfig.GetValue<int>("Port", 5672);
            var username = rabbitMqConfig.GetValue<string>("Username") ?? "guest";
            var password = rabbitMqConfig.GetValue<string>("Password") ?? "guest";
            var exchangeName = rabbitMqConfig.GetValue<string>("ExchangeName") ?? "thethroneofgames.events";
            var dlqExchangeName = rabbitMqConfig.GetValue<string>("DlqExchangeName") ?? "thethroneofgames.dlq";

            services.AddSingleton<IEventBus>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RabbitMqAdapter>>();
                return new RabbitMqAdapter(host, port, username, password, logger, exchangeName, dlqExchangeName);
            });

            return services;
        }

        /// <summary>
        /// Registra o RabbitMQ consumer.
        /// </summary>
        public static IServiceCollection AddRabbitMqConsumer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var rabbitMqConfig = configuration.GetSection("RabbitMq");
            var host = rabbitMqConfig.GetValue<string>("Host") ?? "localhost";
            var port = rabbitMqConfig.GetValue<int>("Port", 5672);
            var username = rabbitMqConfig.GetValue<string>("Username") ?? "guest";
            var password = rabbitMqConfig.GetValue<string>("Password") ?? "guest";

            services.AddSingleton<RabbitMqConsumer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RabbitMqConsumer>>();
                return new RabbitMqConsumer(host, port, username, password, logger);
            });

            return services;
        }
    }
}
