using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameStore.Partidas.Infrastructure.Persistence.Documents;

/// <summary>
/// Formato de persistência do aggregate Partida — separado da entidade de domínio de propósito
/// (Partida.Domain tem construtores/setters privados por design; o driver do MongoDB precisaria
/// de reflexão contra membros privados sem isso). O repositório traduz nos dois sentidos usando
/// as fábricas internas de reidratação (Partida.Reidratar / EquipeSlots.Reidratar).
/// </summary>
public class PartidaDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid JogoId { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<Guid> EquipeA { get; set; } = new();
    public List<Guid> EquipeB { get; set; } = new();
    public List<Guid> Confirmados { get; set; } = new();

    /// <summary>
    /// TTL do Mongo (expireAfterSeconds, ver PartidasMongoContext.EnsureIndexesAsync) — purge
    /// automático 60 dias após a criação, sem job/cron adicional (ver
    /// docs/ai/tasks/prd-partidas.json designDecisions.persistencia).
    /// </summary>
    public DateTime CriadoEmParaTtl { get; set; }
}
