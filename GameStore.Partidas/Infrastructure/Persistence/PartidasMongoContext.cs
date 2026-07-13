using GameStore.Partidas.Infrastructure.Persistence.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace GameStore.Partidas.Infrastructure.Persistence;

/// <summary>
/// Wrapper fino sobre IMongoDatabase — expõe as duas coleções do bounded context e garante os
/// índices TTL (purge automático de 60 dias, ver docs/ai/tasks/prd-partidas.json
/// designDecisions.persistencia) na inicialização, equivalente ao MigrateAsync() dos outros 3
/// serviços (EF Core), só que para Mongo não existe schema migration — só criação de índice.
/// </summary>
public class PartidasMongoContext
{
    public const int PurgeApos60DiasEmSegundos = 60 * 24 * 60 * 60;

    private readonly IMongoDatabase _database;

    static PartidasMongoContext()
    {
        // Desde o MongoDB.Driver 3.x, serializar Guid exige uma representação explícita (antes
        // tinha um default implícito que causava bugs reais de interop entre plataformas) — sem
        // isso, qualquer Find/filter por Guid lança BsonSerializationException em runtime.
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public PartidasMongoContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<PartidaDocument> Partidas => _database.GetCollection<PartidaDocument>("partidas");

    public IMongoCollection<SolicitacaoBuscaDocument> SolicitacoesBusca =>
        _database.GetCollection<SolicitacaoBuscaDocument>("solicitacoesBusca");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromSeconds(PurgeApos60DiasEmSegundos) };

        var partidaIndex = new CreateIndexModel<PartidaDocument>(
            Builders<PartidaDocument>.IndexKeys.Ascending(p => p.CriadoEmParaTtl), ttlOptions);
        await Partidas.Indexes.CreateOneAsync(partidaIndex, cancellationToken: ct);

        var solicitacaoIndex = new CreateIndexModel<SolicitacaoBuscaDocument>(
            Builders<SolicitacaoBuscaDocument>.IndexKeys.Ascending(s => s.CriadoEmParaTtl), ttlOptions);
        await SolicitacoesBusca.Indexes.CreateOneAsync(solicitacaoIndex, cancellationToken: ct);
    }
}
