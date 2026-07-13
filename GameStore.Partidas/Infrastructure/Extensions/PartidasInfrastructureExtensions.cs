using GameStore.Common.Events;
using GameStore.Common.Messaging;
using GameStore.Partidas.Application.Ports;
using GameStore.Partidas.Application.Services;
using GameStore.Partidas.Application.UseCases;
using GameStore.Partidas.Domain.Repositories;
using GameStore.Partidas.Infrastructure.Gateways;
using GameStore.Partidas.Infrastructure.Persistence;
using GameStore.Partidas.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameStore.Partidas.Infrastructure.Extensions;

public static class PartidasInfrastructureExtensions
{
    public static IServiceCollection AddPartidasInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB — não EF Core/Postgres (ver docs/ai/tasks/prd-partidas.json
        // designDecisions.persistencia). Singleton: MongoClient/IMongoDatabase são thread-safe
        // e feitos para serem reutilizados, diferente do DbContext dos outros 3 serviços.
        var mongoConnectionString = configuration.GetConnectionString("PartidasMongoConnection")
            ?? configuration["Mongo:ConnectionString"]
            ?? "mongodb://localhost:27017";
        var mongoDatabaseName = configuration["Mongo:DatabaseName"] ?? "gamestore_partidas";
        services.AddSingleton(new PartidasMongoContext(mongoConnectionString, mongoDatabaseName));

        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<ISolicitacaoBuscaRepository, SolicitacaoBuscaRepository>();

        // Event Bus — mesmo padrão condicional dos outros 3 serviços: RabbitMQ real em
        // desenvolvimento/produção, SimpleEventBus (em memória) quando testes sobem via
        // WebApplicationFactory sem broker.
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

        // Gateway HTTP síncrono para GameStore.Usuarios (posse do jogo) — ver IUsuariosGateway.
        var usuariosBaseUrl = configuration["Services:UsuariosBaseUrl"] ?? "http://usuarios-api";
        services.AddHttpClient<IUsuariosGateway, UsuariosHttpGateway>(client =>
        {
            client.BaseAddress = new Uri(usuariosBaseUrl);
        });

        services.AddScoped<MotorDePareamento>();
        services.AddScoped<BuscarPartidaUseCase>();
        services.AddScoped<ConfirmarPartidaUseCase>();
        services.AddScoped<DesistirPartidaUseCase>();
        services.AddScoped<ConsultarStatusUseCase>();

        return services;
    }
}
