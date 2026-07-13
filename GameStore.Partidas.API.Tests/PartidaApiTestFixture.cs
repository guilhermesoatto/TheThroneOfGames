using Testcontainers.MongoDb;
using Xunit;

namespace GameStore.Partidas.API.Tests;

/// <summary>
/// Compartilhada por todos os [Fact] da classe via IClassFixture — sobe UM container MongoDB e
/// UMA WebApplicationFactory (não uma por teste). Além de mais rápido, evita que o
/// KestrelMetricServer (porta fixa, ver Program.cs) tente bindar a mesma porta várias vezes no
/// mesmo processo — cada teste usa jogadorId/jogoId aleatórios, então compartilhar o banco entre
/// eles não causa interferência.
/// </summary>
public class PartidaApiTestFixture : IAsyncLifetime
{
    private MongoDbContainer _mongoContainer = null!;
    public PartidasWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder("mongo:7.0").Build();
        await _mongoContainer.StartAsync();

        Factory = new PartidasWebApplicationFactory(_mongoContainer.GetConnectionString());
        await Factory.EnsureMongoIndexesAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }
}
