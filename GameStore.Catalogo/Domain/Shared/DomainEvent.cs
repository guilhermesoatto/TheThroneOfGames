namespace GameStore.Catalogo.Domain.Shared;

public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required string EventType { get; init; }
    public required int EventVersion { get; init; }
    public required Guid AggregateId { get; init; }
    public required string AggregateName { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public required string CorrelationId { get; init; }
    public required IReadOnlyDictionary<string, object> Payload { get; init; }
}
