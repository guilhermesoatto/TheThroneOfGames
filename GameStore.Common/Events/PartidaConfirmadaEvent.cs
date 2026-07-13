namespace GameStore.Common.Events
{
    /// <summary>
    /// Evento publicado quando TODOS os jogadores de uma Partida confirmaram — a partida
    /// efetivamente começa. Contexto: matchmaking (GameStore.Partidas).
    /// </summary>
    public record PartidaConfirmadaEvent(
        Guid PartidaId,
        Guid JogoId,
        IReadOnlyList<Guid> JogadoresIds
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventName => nameof(PartidaConfirmadaEvent);
    }
}
