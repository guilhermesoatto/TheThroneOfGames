namespace TheThroneOfGames.Domain.Events
{
    /// <summary>
    /// Interface base para handlers que processam eventos de domínio.
    /// </summary>
    /// <typeparam name="TEvent">Tipo de evento que este handler processa</typeparam>
    public interface IEventHandler<TEvent> where TEvent : IDomainEvent
    {
        /// <summary>
        /// Processa um evento de domínio.
        /// </summary>
        /// <param name="domainEvent">O evento a processar</param>
        /// <returns>Task assíncrona representando o processamento</returns>
        Task HandleAsync(TEvent domainEvent);
    }
}
