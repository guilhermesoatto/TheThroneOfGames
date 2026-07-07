using GameStore.Vendas.Domain.Shared;

namespace GameStore.Vendas.Domain.EventSourcing;

/// <summary>
/// Evento de domínio concreto para o aggregate Pedido — usado pelo Event Store (esboço de Event Sourcing).
/// Um único tipo de registro cobre todo o ciclo de vida do Pedido; o campo <see cref="DomainEvent.EventType"/>
/// diferencia a transição de estado (ex: "PedidoCriado", "PedidoFinalizado", "PedidoCancelado").
/// </summary>
public sealed record PedidoDomainEvent : DomainEvent;
