using TheThroneOfGames.Domain.Events;

namespace GameStore.Catalogo.Domain.Events
{
    /// <summary>
    /// Evento publicado quando um jogo é comprado com sucesso.
    /// Contexto: Quando um usuário completa uma compra.
    /// </summary>
    public record GameCompradoEvent(
        Guid GameId,
        Guid UserId,
        decimal Preco,
        string NomeJogo
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }
}
