using GameStore.Vendas.Domain.Shared;

namespace GameStore.Vendas.Domain.EventSourcing;

/// <summary>
/// Event Store append-only — registra toda mudança de estado de um aggregate como um evento imutável.
/// Esboço de Event Sourcing: hoje é usado como log de auditoria (append), sem replay/reconstrução de
/// estado a partir dos eventos (o aggregate ainda é persistido via snapshot, ver <see cref="GameStore.Vendas.Domain.Repositories.IPedidoRepository"/>).
/// </summary>
public interface IEventStore
{
    Task AppendAsync(DomainEvent domainEvent, CancellationToken ct = default);

    Task<IReadOnlyList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct = default);
}
