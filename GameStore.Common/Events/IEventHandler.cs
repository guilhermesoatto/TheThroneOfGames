namespace GameStore.Common.Events
{
    /// <summary>
    /// Interface para handlers de eventos de domínio.
    /// </summary>
    /// <typeparam name="TEvent">Tipo de evento que o handler processa</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        /// <summary>
        /// Processa um evento de domínio.
        /// </summary>
        /// <param name="domainEvent">O evento a processar</param>
        /// <returns>Task assíncrona representando o processamento</returns>
        Task HandleAsync(TEvent domainEvent);
    }
}
