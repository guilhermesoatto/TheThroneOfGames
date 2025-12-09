namespace TheThroneOfGames.Domain.Events
{
    /// <summary>
    /// Interface base para eventos de domínio.
    /// Todo evento que representa uma mudança significativa no domínio deve implementar esta interface.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// ID único do evento.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Timestamp do quando o evento foi criado.
        /// </summary>
        DateTime OccurredAt { get; }

        /// <summary>
        /// Nome do evento (útil para logging e debugging).
        /// </summary>
        string EventName => GetType().Name;
    }
}
