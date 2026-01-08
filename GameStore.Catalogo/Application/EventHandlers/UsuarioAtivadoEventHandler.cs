using System;
using System.Threading.Tasks;
using GameStore.Common.Events;

namespace GameStore.Catalogo.Application.EventHandlers
{
    /// <summary>
    /// Handler que reage quando um usuário é ativado.
    /// Sincroniza informações do usuário no contexto de Catálogo.
    /// </summary>
    public class UsuarioAtivadoEventHandler : IEventHandler<UsuarioAtivadoEvent>
    {
        public Task HandleAsync(UsuarioAtivadoEvent domainEvent)
        {
            // Log ou processamento quando usuário é ativado no contexto de Usuários
            Console.WriteLine($"[Catálogo] Notificação: Usuário {domainEvent.Nome} ({domainEvent.Email}) foi ativado.");
            
            // Aqui poderíamos:
            // - Sincronizar dados do usuário
            // - Preparar recomendações iniciais
            // - Enviar email de boas-vindas
            // - Registrar evento para auditoria
            
            return Task.CompletedTask;
        }
    }
}
