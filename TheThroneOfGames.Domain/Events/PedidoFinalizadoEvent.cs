namespace TheThroneOfGames.Domain.Events
{
    /// <summary>
    /// Evento publicado quando um pedido é finalizado com sucesso.
    /// Contexto: Quando um pedido de compra é concluído e processado.
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
    }
}
