using MongoDB.Bson.Serialization.Attributes;

namespace GameStore.Partidas.Infrastructure.Persistence.Documents;

public class SolicitacaoBuscaDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid JogadorId { get; set; }
    public Guid JogoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public Guid? PartidaId { get; set; }

    /// <summary>TTL do Mongo — mesmo purge de 60 dias que PartidaDocument, ver PartidasMongoContext.</summary>
    public DateTime CriadoEmParaTtl { get; set; }
}
