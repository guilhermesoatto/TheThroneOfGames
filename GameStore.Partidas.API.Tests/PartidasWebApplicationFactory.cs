using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GameStore.Common.Events;
using GameStore.Common.Messaging;
using GameStore.Partidas.Application.Ports;
using GameStore.Partidas.Infrastructure.Persistence;

namespace GameStore.Partidas.API.Tests;

public class PartidasWebApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly string _mongoConnectionString;
    private readonly bool _jogadorPossuiTodosOsJogos;

    public PartidasWebApplicationFactory(string mongoConnectionString, bool jogadorPossuiTodosOsJogos = true)
    {
        _mongoConnectionString = mongoConnectionString;
        _jogadorPossuiTodosOsJogos = jogadorPossuiTodosOsJogos;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // PartidasMongoContext é lido eagerly em Program.cs (AddSingleton(new
            // PartidasMongoContext(configuration...))) — diferente do AddDbContext<T> dos
            // outros 3 serviços (lazy, resolvido só quando o DbContext é injetado pela 1ª vez),
            // então ConfigureAppConfiguration não chega a tempo de mudar a connection string
            // que Program.cs já usou. Precisa substituir o registro do serviço diretamente.
            var mongoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PartidasMongoContext));
            if (mongoDescriptor != null) services.Remove(mongoDescriptor);
            services.AddSingleton(new PartidasMongoContext(_mongoConnectionString, $"gamestore_partidas_test_{Guid.NewGuid():N}"));

            // Mesmo racional de VendasWebApplicationFactory: troca RabbitMqAdapter por
            // SimpleEventBus (em memória) para não exigir um broker real nos testes.
            var eventBusDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
            if (eventBusDescriptor != null) services.Remove(eventBusDescriptor);
            services.AddSingleton<IEventBus, SimpleEventBus>();

            // Troca a chamada HTTP real a Usuarios por um dublê — ver StubUsuariosGateway.
            var gatewayDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUsuariosGateway));
            if (gatewayDescriptor != null) services.Remove(gatewayDescriptor);
            services.AddSingleton<IUsuariosGateway>(new StubUsuariosGateway(_jogadorPossuiTodosOsJogos));
        });
    }

    /// <summary>Garante os índices TTL antes dos testes usarem o Mongo (mesmo passo que Program.cs faz no startup real).</summary>
    public async Task EnsureMongoIndexesAsync()
    {
        using var scope = Services.CreateScope();
        var mongoContext = scope.ServiceProvider.GetRequiredService<PartidasMongoContext>();
        await mongoContext.EnsureIndexesAsync();
    }
}
