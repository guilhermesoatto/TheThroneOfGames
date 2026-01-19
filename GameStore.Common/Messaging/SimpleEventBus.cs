using GameStore.Common.Events;

namespace GameStore.Common.Messaging
{
    /// <summary>
    /// Implementação simples de um event bus em memória.
    /// Usa um dicionário para manter registro de handlers por tipo de evento.
    /// Implementa thread-safety com lock.
    /// </summary>
    public class SimpleEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Registra um handler para um tipo de evento específico.
        /// </summary>
        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                var eventType = typeof(TEvent);
                
                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<Delegate>();

                // Convertemos o handler para um delegate genérico
                Delegate del = new Func<TEvent, Task>(handler.HandleAsync);
                _handlers[eventType].Add(del);
            }
        }

        /// <summary>
        /// Remove o registro de um handler.
        /// </summary>
        public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                var eventType = typeof(TEvent);
                
                if (!_handlers.ContainsKey(eventType))
                    return;

                Delegate del = new Func<TEvent, Task>(handler.HandleAsync);
                _handlers[eventType].Remove(del);

                // Remover tipo de evento se não houver mais handlers
                if (_handlers[eventType].Count == 0)
                    _handlers.Remove(eventType);
            }
        }

        /// <summary>
        /// Publica um evento para todos os handlers registrados.
        /// Executa handlers de forma sequencial (não paralela).
        /// </summary>
        public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            var eventType = typeof(TEvent);
            List<Delegate> handlers;

            lock (_lockObject)
            {
                if (!_handlers.ContainsKey(eventType))
                    return; // Nenhum handler registrado

                handlers = new List<Delegate>(_handlers[eventType]);
            }

            // Executar handlers fora do lock para evitar deadlocks
            foreach (var handlerDelegate in handlers)
            {
                if (handlerDelegate is Func<TEvent, Task> handlerFunc)
                {
                    await handlerFunc(domainEvent);
                }
            }
        }

        /// <summary>
        /// Obtém o número de handlers registrados para um tipo de evento.
        /// </summary>
        public int GetHandlerCount<TEvent>() where TEvent : IDomainEvent
        {
            lock (_lockObject)
            {
                var eventType = typeof(TEvent);
                return _handlers.ContainsKey(eventType) ? _handlers[eventType].Count : 0;
            }
        }
    }
}
