using System.Threading.Tasks;
using GameStore.Common.Events;

namespace GameStore.Usuarios.Application.EventHandlers
{
    /// <summary>
    /// Lightweight handler in the Usuarios context to satisfy tests that expect
    /// a handler in this namespace. It performs no work beyond acknowledging the event.
    /// </summary>
    public class UsuarioAtivadoEventHandler : IEventHandler<UsuarioAtivadoEvent>
    {
        public Task HandleAsync(UsuarioAtivadoEvent domainEvent)
        {
            // No-op: tests only assert that calling the handler does not throw.
            return Task.CompletedTask;
        }
    }
}
