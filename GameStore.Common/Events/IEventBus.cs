namespace GameStore.Common.Events
{
    /// <summary>
    /// Interface para o barramento de eventos que publica e subscribe a eventos de domínio.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Registra um handler para um tipo de evento específico.
        /// </summary>
        /// <typeparam name="TEvent">Tipo de evento</typeparam>
        /// <param name="handler">Handler a registrar</param>
        void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;

        /// <summary>
        /// Remove o registro de um handler.
        /// </summary>
        /// <typeparam name="TEvent">Tipo de evento</typeparam>
        /// <param name="handler">Handler a remover</param>
        void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;

        /// <summary>
        /// Publica um evento de domínio para todos os handlers registrados.
        /// </summary>
        /// <typeparam name="TEvent">Tipo de evento</typeparam>
        /// <param name="domainEvent">O evento a publicar</param>
        /// <returns>Task assíncrona representando a publicação</returns>
        Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;

        /// <summary>
        /// Obtém o número de handlers registrados para um tipo de evento.
        /// </summary>
        /// <typeparam name="TEvent">Tipo de evento</typeparam>
        /// <returns>Número de handlers registrados</returns>
        int GetHandlerCount<TEvent>() where TEvent : IDomainEvent;
    }
}
