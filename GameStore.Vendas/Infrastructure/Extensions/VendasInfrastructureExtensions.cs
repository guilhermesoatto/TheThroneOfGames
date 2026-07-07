using GameStore.Vendas.Domain.Repositories;
using GameStore.Vendas.Domain.EventSourcing;
using GameStore.Vendas.Infrastructure.Persistence;
using GameStore.Vendas.Infrastructure.Repository;
using GameStore.Vendas.Infrastructure.EventSourcing;
using GameStore.Vendas.Application.Commands;
using GameStore.Vendas.Application.Handlers;
using GameStore.CQRS.Abstractions;
using GameStore.Common.Events;
using GameStore.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameStore.Vendas.Infrastructure.Extensions
{
    public static class VendasInfrastructureExtensions
    {
        public static IServiceCollection AddVendasInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext - sempre usar PostgreSQL
            services.AddDbContext<VendasDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("VendasConnection") ??
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("GameStore.Vendas")));

            // Repositories
            services.AddScoped<IPedidoRepository, PedidoRepository>();

            // Event Store (esboço de Event Sourcing)
            services.AddScoped<IEventStore, EventStore>();

            // Event Bus — publica PedidoFinalizadoEvent no RabbitMQ para Usuarios e as Functions
            // serverless. Usa SimpleEventBus (em memória) quando EventBus:UseRabbitMq=false — evita
            // exigir um broker real em cenários locais/sem docker-compose (ex.: dotnet run isolado).
            if (configuration.GetValue<bool>("EventBus:UseRabbitMq"))
            {
                services.AddSingleton<IEventBus>(provider =>
                {
                    var rabbitMqConfig = configuration.GetSection("EventBus:RabbitMq");
                    var host = rabbitMqConfig.GetValue<string>("HostName") ?? "localhost";
                    var port = rabbitMqConfig.GetValue<int>("Port", 5672);
                    var username = rabbitMqConfig.GetValue<string>("UserName") ?? "guest";
                    var password = rabbitMqConfig.GetValue<string>("Password") ?? "guest";
                    var logger = provider.GetRequiredService<ILogger<RabbitMqAdapter>>();
                    return new RabbitMqAdapter(host, port, username, password, logger);
                });
            }
            else
            {
                services.AddSingleton<IEventBus, SimpleEventBus>();
            }

            // Command Handlers
            services.AddScoped<ICommandHandler<CriarPedidoCommand>, CriarPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<AdicionarItemPedidoCommand>, AdicionarItemPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<RemoverItemPedidoCommand>, RemoverItemPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<FinalizarPedidoCommand>, FinalizarPedidoCommandHandler>();
            services.AddScoped<ICommandHandler<CancelarPedidoCommand>, CancelarPedidoCommandHandler>();

            return services;
        }
    }
}