using System.Threading.Tasks;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Interface para consumers de eventos de domínio via mensageria.
    /// </summary>
    public interface IEventConsumer
    {
        /// <summary>
        /// Inicia o consumo de mensagens para este consumer.
        /// </summary>
        Task StartConsumingAsync();

        /// <summary>
        /// Para o consumo de mensagens.
        /// </summary>
        Task StopConsumingAsync();
    }

    /// <summary>
    /// Consumer genérico para eventos de domínio.
    /// </summary>
    /// <typeparam name="TEvent">Tipo do evento a ser consumido</typeparam>
    public interface IEventConsumer<TEvent> : IEventConsumer where TEvent : IDomainEvent
    {
        /// <summary>
        /// Processa um evento específico.
        /// </summary>
        Task ProcessEventAsync(TEvent domainEvent);
    }
}