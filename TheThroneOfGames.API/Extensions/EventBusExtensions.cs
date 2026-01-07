using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Events;
using GameStore.Common.Messaging;
using GameStore.Catalogo.Application.EventConsumers;
using GameStore.Usuarios.Application.EventConsumers;

namespace TheThroneOfGames.API.Extensions
{
    public static class EventBusExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            var useRabbitMq = configuration.GetValue<bool>("EventBus:UseRabbitMq", false);

            if (useRabbitMq)
            {
                // Configurar RabbitMQ Event Bus
                services.AddSingleton<IEventBus>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<RabbitMqAdapter>>();
                    var host = configuration.GetValue<string>("EventBus:RabbitMq:Host", "localhost");
                    var port = configuration.GetValue<int>("EventBus:RabbitMq:Port", 5672);
                    var username = configuration.GetValue<string>("EventBus:RabbitMq:Username", "guest");
                    var password = configuration.GetValue<string>("EventBus:RabbitMq:Password", "guest");

                    return new RabbitMqAdapter(host, port, username, password, logger);
                });

                // Registrar consumers como hosted services
                RegisterEventConsumers(services, configuration);
            }
            else
            {
                // Usar SimpleEventBus (in-memory) para desenvolvimento
                services.AddSingleton<IEventBus, SimpleEventBus>();
            }

            return services;
        }

        private static void RegisterEventConsumers(IServiceCollection services, IConfiguration configuration)
        {
            var host = configuration.GetValue<string>("EventBus:RabbitMq:Host", "localhost");
            var port = configuration.GetValue<int>("EventBus:RabbitMq:Port", 5672);
            var username = configuration.GetValue<string>("EventBus:RabbitMq:Username", "guest");
            var password = configuration.GetValue<string>("EventBus:RabbitMq:Password", "guest");

            // Registrar consumers para Catalogo
            services.AddSingleton<IEventConsumer>(sp =>
                new UsuarioAtivadoEventConsumer(host, port, username, password,
                    sp.GetRequiredService<ILogger<UsuarioAtivadoEventConsumer>>()));

            // Registrar consumers para Usuarios
            services.AddSingleton<IEventConsumer>(sp =>
                new GameCompradoEventConsumer(host, port, username, password,
                    sp.GetRequiredService<ILogger<GameCompradoEventConsumer>>()));

            services.AddSingleton<IEventConsumer>(sp =>
                new PedidoFinalizadoEventConsumer(host, port, username, password,
                    sp.GetRequiredService<ILogger<PedidoFinalizadoEventConsumer>>()));

            // Registrar o hosted service que gerencia todos os consumers
            services.AddHostedService<EventConsumerService>();
        }
    }
}