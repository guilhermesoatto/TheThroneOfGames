using System.Text.Json;
using GameStore.Vendas.Domain.EventSourcing;
using GameStore.Vendas.Domain.Shared;
using GameStore.Vendas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Vendas.Infrastructure.EventSourcing;

public class EventStore : IEventStore
{
    private readonly VendasDbContext _context;

    public EventStore(VendasDbContext context)
    {
        _context = context;
    }

    public async Task AppendAsync(DomainEvent domainEvent, CancellationToken ct = default)
    {
        var entry = new EventStoreEntry
        {
            EventId = domainEvent.EventId,
            EventType = domainEvent.EventType,
            EventVersion = domainEvent.EventVersion,
            AggregateId = domainEvent.AggregateId,
            AggregateName = domainEvent.AggregateName,
            OccurredAt = domainEvent.OccurredAt,
            CorrelationId = domainEvent.CorrelationId,
            PayloadJson = JsonSerializer.Serialize(domainEvent.Payload)
        };

        _context.EventStore.Add(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct = default)
    {
        var entries = await _context.EventStore
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(ct);

        return entries.Select(e => (DomainEvent)new PedidoDomainEvent
        {
            EventId = e.EventId,
            EventType = e.EventType,
            EventVersion = e.EventVersion,
            AggregateId = e.AggregateId,
            AggregateName = e.AggregateName,
            OccurredAt = e.OccurredAt,
            CorrelationId = e.CorrelationId,
            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(e.PayloadJson) ?? new Dictionary<string, object>()
        }).ToList();
    }
}
