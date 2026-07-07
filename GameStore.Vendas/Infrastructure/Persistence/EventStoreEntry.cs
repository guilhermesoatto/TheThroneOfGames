namespace GameStore.Vendas.Infrastructure.Persistence;

/// <summary>
/// Registro persistido do Event Store (append-only). Uma linha por evento de domínio publicado.
/// </summary>
public class EventStoreEntry
{
    public Guid EventId { get; set; }
    public required string EventType { get; set; }
    public int EventVersion { get; set; }
    public Guid AggregateId { get; set; }
    public required string AggregateName { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string CorrelationId { get; set; }
    public required string PayloadJson { get; set; }
}
