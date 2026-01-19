namespace GameStore.Common.Events
{
    /// <summary>
    /// Evento publicado quando um pedido é finalizado com sucesso.
    /// Contexto: Quando pagamento é processado e pedido é completado.
    /// </summary>
    public record PedidoFinalizadoEvent(
        Guid PedidoId,
        Guid UserId,
        decimal TotalPrice,
        int ItemCount
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventName => nameof(PedidoFinalizadoEvent);
    }
}
