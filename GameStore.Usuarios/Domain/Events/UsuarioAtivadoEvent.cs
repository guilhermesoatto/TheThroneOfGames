using TheThroneOfGames.Domain.Events;

namespace GameStore.Usuarios.Domain.Events
{
    /// <summary>
    /// Evento publicado quando um usuário é ativado com sucesso.
    /// Contexto: Quando um usuário clica no link de ativação de e-mail.
    /// </summary>
    public record UsuarioAtivadoEvent(
        Guid UsuarioId,
        string Email,
        string Nome
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }
}
