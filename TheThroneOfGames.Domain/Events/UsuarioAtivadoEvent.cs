namespace TheThroneOfGames.Domain.Events
{
    /// <summary>
    /// Evento publicado quando um usuário é ativado com sucesso.
    /// Contexto: Quando um usuário clica no link de ativação de e-mail.
    /// Compatível com testes que usam inicializador de objeto (object initializer).
    /// </summary>
    public class UsuarioAtivadoEvent : IDomainEvent
    {
        // Core domain properties
        public Guid UsuarioId { get; set; }
        public string? Email { get; set; }
        public string? Nome { get; set; }

        // Friendly aliases used by some tests/consumers
        public Guid UserId { get => UsuarioId; set => UsuarioId = value; }
        public DateTime Timestamp { get => OccurredAt; set => OccurredAt = value; }

        // Event metadata
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        // Parameterless constructor for object-initializer usage in tests
        public UsuarioAtivadoEvent() { }

        // Backwards-compatible constructor used across the codebase
        public UsuarioAtivadoEvent(Guid UsuarioId, string Email, string Nome)
        {
            this.UsuarioId = UsuarioId;
            this.Email = Email;
            this.Nome = Nome;
            this.OccurredAt = DateTime.UtcNow;
        }
    }
}
