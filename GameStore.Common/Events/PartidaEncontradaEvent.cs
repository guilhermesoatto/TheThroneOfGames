namespace GameStore.Common.Events
{
    /// <summary>
    /// Evento publicado quando duas SolicitacaoBusca são pareadas e uma Partida é formada
    /// (1v1 imediato — ver GameStore.Partidas). Contexto: matchmaking.
    /// </summary>
    public record PartidaEncontradaEvent(
        Guid PartidaId,
        Guid JogoId,
        Guid JogadorAId,
        Guid JogadorBId
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventName => nameof(PartidaEncontradaEvent);
    }
}
