namespace GameStore.Common.Events
{
    /// <summary>
    /// Evento publicado quando uma Partida é desfeita por desistência de um jogador antes da
    /// confirmação mútua. Contexto: matchmaking (GameStore.Partidas) — o jogador restante volta
    /// para uma nova SolicitacaoBusca(Aguardando) e o motor de pareamento tenta de novo.
    /// </summary>
    public record PartidaCanceladaEvent(
        Guid PartidaId,
        Guid JogoId,
        Guid JogadorQueDesistiuId
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventName => nameof(PartidaCanceladaEvent);
    }
}
