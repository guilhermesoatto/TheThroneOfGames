namespace GameStore.Common.Events
{
    /// <summary>
    /// Evento publicado quando o perfil de um usuário é atualizado.
    /// Contexto: Quando um usuário altera nome ou e-mail na conta.
    /// </summary>
    public record UsuarioPerfillAtualizadoEvent(
        Guid UsuarioId,
        string NovoNome,
        string NovoEmail
    ) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventName => nameof(UsuarioPerfillAtualizadoEvent);
    }
}
